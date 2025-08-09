using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using GoodWin.Core;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace GoodWin.Debuffs.Hard;

/// <summary>
/// "My Little Pony" debuff – renders full screen hue shift over the game using DirectX.
/// </summary>
[DebuffSchedule(DebuffPhase.Hard, 0, 999, 60)]
public class RainbowDebuff : DebuffBase, IOverlayDebuff
{
    private RainbowRenderer? _renderer;

    public override string Name => "My Little Pony";

    public override void Apply()
    {
        _renderer = new RainbowRenderer();
        _renderer.Start();
        Console.WriteLine("[Rainbow] applied");
    }

    public override void Remove()
    {
        _renderer?.Dispose();
        _renderer = null;
        Console.WriteLine("[Rainbow] removed");
    }

    private sealed class RainbowRenderer : IDisposable
    {
        // DirectX resources
        private readonly ID3D11Device _device;
        private readonly ID3D11DeviceContext _context;
        private readonly IDXGISwapChain1 _swapChain;
        private readonly IDXGIOutputDuplication _duplication;
        private readonly ID3D11RenderTargetView _rtv;
        private readonly ID3D11VertexShader _vs;
        private readonly ID3D11PixelShader _ps;
        private readonly ID3D11Buffer _vb;
        private readonly ID3D11SamplerState _sampler;
        private readonly ID3D11Buffer _cbuffer;

        private readonly Thread _thread;
        private bool _running;

        private readonly IntPtr _hwnd;

        [StructLayout(LayoutKind.Sequential)]
        private struct Vertex
        {
            public Vector3 Position;
            public Vector2 TexCoord;
            public Vertex(float x, float y, float u, float v)
            {
                Position = new Vector3(x, y, 0f);
                TexCoord = new Vector2(u, v);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HueConstant
        {
            public float Hue;
            private readonly Vector3 _padding;
        }

        public RainbowRenderer()
        {
            _hwnd = Native.CreateWindow();

            // Device + swap chain
            DXGI.CreateDXGIFactory1(out IDXGIFactory2 factory).CheckError();
            D3D11.D3D11CreateDevice(null, DriverType.Hardware, DeviceCreationFlags.BgraSupport, null, out _device).CheckError();
            _context = _device.ImmediateContext;

            int width = Native.GetSystemMetrics(0);
            int height = Native.GetSystemMetrics(1);

            var swapDesc = new SwapChainDescription1
            {
                Width = width,
                Height = height,
                Format = Format.B8G8R8A8_UNorm,
                BufferUsage = Usage.RenderTargetOutput,
                BufferCount = 2,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.FlipDiscard,
                AlphaMode = AlphaMode.Ignore
            };

            _swapChain = factory.CreateSwapChainForHwnd(_device, _hwnd, swapDesc);
            using var backBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
            _rtv = _device.CreateRenderTargetView(backBuffer);

            // Desktop duplication
            factory.EnumAdapters1(0, out IDXGIAdapter1 adapter).CheckError();
            adapter.EnumOutputs(0, out IDXGIOutput output).CheckError();
            var output1 = output.QueryInterface<IDXGIOutput1>();
            _duplication = output1.DuplicateOutput(_device);

            // Compile shaders
            string shaderPath = Path.Combine(AppContext.BaseDirectory, "HueShift.hlsl");
            var shaderSrc = File.ReadAllText(shaderPath);
            var vsCode = ShaderCompiler.Compile(shaderSrc, "VSMain", "vs_5_0");
            var psCode = ShaderCompiler.Compile(shaderSrc, "PSMain", "ps_5_0");
            _vs = _device.CreateVertexShader(vsCode);
            _ps = _device.CreatePixelShader(psCode);

            // Fullscreen quad vertex buffer
            var vertices = new[]
            {
                new Vertex(-1, -1, 0, 1),
                new Vertex(-1,  1, 0, 0),
                new Vertex( 1, -1, 1, 1),
                new Vertex( 1,  1, 1, 0)
            };
            _vb = _device.CreateBuffer(vertices, BindFlags.VertexBuffer);

            _sampler = _device.CreateSamplerState(new SamplerDescription(Filter.MinMagMipLinear, TextureAddressMode.Clamp, TextureAddressMode.Clamp, TextureAddressMode.Clamp));

            _cbuffer = _device.CreateBuffer(new BufferDescription
            {
                ByteWidth = Marshal.SizeOf<HueConstant>(),
                Usage = ResourceUsage.Dynamic,
                BindFlags = BindFlags.ConstantBuffer,
                CPUAccessFlags = CpuAccessFlags.Write
            });

            _thread = new Thread(RenderLoop) { IsBackground = true };
        }

        public void Start()
        {
            _running = true;
            _thread.Start();
        }

        private void RenderLoop()
        {
            float hue = 0f;
            var stride = Marshal.SizeOf<Vertex>();
            var offset = 0;
            while (_running)
            {
                Native.PumpMessages();
                try
                {
                    _duplication.AcquireNextFrame(16, out _, out IDXGIResource resource);
                    using var tex = resource.QueryInterface<ID3D11Texture2D>();
                    using var srv = _device.CreateShaderResourceView(tex);

                    hue += 0.01f;
                    var data = new HueConstant { Hue = hue };
                    _context.UpdateSubresource(ref data, _cbuffer);

                    _context.OMSetRenderTargets(_rtv);
                    _context.ClearRenderTargetView(_rtv, new Color4(0, 0, 0, 0));
                    _context.IASetVertexBuffer(0, _vb, stride, offset);
                    _context.IASetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
                    _context.VSSetShader(_vs);
                    _context.PSSetShader(_ps);
                    _context.PSSetSamplers(0, new[] { _sampler });
                    _context.PSSetShaderResources(0, new[] { srv });
                    _context.PSSetConstantBuffers(0, new[] { _cbuffer });

                    _context.Draw(4, 0);
                    _swapChain.Present(1, PresentFlags.None);
                    resource.Dispose();
                    _duplication.ReleaseFrame();
                }
                catch
                {
                    Thread.Sleep(16);
                }
            }
        }

        public void Dispose()
        {
            _running = false;
            _thread.Join();
            _swapChain.Dispose();
            _duplication.Dispose();
            _rtv.Dispose();
            _ps.Dispose();
            _vs.Dispose();
            _vb.Dispose();
            _sampler.Dispose();
            _cbuffer.Dispose();
            _context.Dispose();
            _device.Dispose();
            Native.DestroyWindow(_hwnd);
        }
    }

    private static class Native
    {
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_POPUP = unchecked((int)0x80000000);
        private const int WS_VISIBLE = 0x10000000;
        private const int SW_SHOW = 5;
        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WNDCLASSEX
        {
            public uint cbSize;
            public uint style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string? lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public UIntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public Point pt;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Point
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName,
            int dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyWindowNative(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetricsNative(int nIndex);

        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr GetModuleHandle(string? lpModuleName);
        [DllImport("user32.dll")] private static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool PeekMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);
        [DllImport("user32.dll")] private static extern bool TranslateMessage(ref MSG lpMsg);
        [DllImport("user32.dll")] private static extern IntPtr DispatchMessage(ref MSG lpMsg);

        private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private static readonly WndProc WndProcDelegate = DefWindowProc;

        public static IntPtr CreateWindow()
        {
            var hInstance = GetModuleHandle(null);
            var cls = new WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(WndProcDelegate),
                hInstance = hInstance,
                lpszClassName = "RainbowOverlay"
            };
            RegisterClassEx(ref cls);
            int width = GetSystemMetricsNative(SM_CXSCREEN);
            int height = GetSystemMetricsNative(SM_CYSCREEN);
            var hwnd = CreateWindowEx(WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOPMOST | WS_EX_NOACTIVATE,
                cls.lpszClassName, string.Empty, WS_POPUP | WS_VISIBLE, 0, 0, width, height, IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);
            ShowWindow(hwnd, SW_SHOW);
            return hwnd;
        }

        public static void DestroyWindow(IntPtr hwnd) => DestroyWindowNative(hwnd);

        public static int GetSystemMetrics(int index) => GetSystemMetricsNative(index);

        public static void PumpMessages()
        {
            while (PeekMessage(out var msg, IntPtr.Zero, 0, 0, 1))
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
        }
    }

    private static class ShaderCompiler
    {
        public static Blob Compile(string source, string entry, string profile)
        {
            var result = Vortice.D3DCompiler.Compiler.Compile(source, entry, profile);
            if (result.Bytecode == null) throw new InvalidOperationException(result.Message);
            return result.Bytecode;
        }
    }
}

