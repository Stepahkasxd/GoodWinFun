using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using GoodWin.Core;
using GoodWin.Gui.Models;
using GoodWin.Gui.Views;

namespace GoodWin.Gui.Services
{
    public class RouletteService
    {
        private readonly UserSettingsService _settings = new("usersettings.json");
        private RouletteWindow? _window;

        public void ShowRouletteForEvents(IEnumerable<Event> events, Action? onCompleted = null)
        {
            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() => ShowRouletteForEvents(events, onCompleted));
                return;
            }

            _window ??= new RouletteWindow();

            var eventList = events.ToList();
            var settingsSegments = _settings.Settings.Roulette.Segments;
            var segments = new ObservableCollection<RouletteSegment>();

            if (settingsSegments.Count == 0)
            {
                string[] defaults = { "#FF0000", "#00FF00", "#0000FF", "#FFFF00", "#FF00FF", "#00FFFF" };
                for (int i = 0; i < eventList.Count; i++)
                {
                    segments.Add(new RouletteSegment
                    {
                        ColorHex = defaults[i % defaults.Length],
                        Opacity = 1.0,
                        Label = eventList[i].Name,
                        AssociatedEvent = eventList[i]
                    });
                }
            }
            else
            {
                foreach (var s in settingsSegments)
                {
                    segments.Add(new RouletteSegment
                    {
                        ColorHex = s.ColorHex,
                        Opacity = s.Opacity,
                        ImagePath = s.ImagePath,
                        Label = s.Label
                    });
                }

                for (int i = 0; i < eventList.Count && i < segments.Count; i++)
                {
                    segments[i].Label = eventList[i].Name;
                    segments[i].AssociatedEvent = eventList[i];
                }
            }

            if (segments.Count == 0)
            {
                onCompleted?.Invoke();
                return;
            }

            _window.WheelControl.Segments = segments;
            _window.WheelControl.WheelOpacity = _settings.Settings.Roulette.WheelOpacity;

            _window.Show();
            _window.WheelControl.Spin(_settings.Settings.Roulette.SpinDuration, seg =>
            {
                async void Run()
                {
                    try
                    {
                        var notify = new DebuffNotificationWindow(seg.Label);
                        notify.Show();
                        await System.Threading.Tasks.Task.Delay(3000);
                        notify.Close();
                        seg.AssociatedEvent?.Invoke();
                        onCompleted?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                    finally
                    {
                        _window.Hide();
                    }
                }
                Run();
            });
        }
    }
}
