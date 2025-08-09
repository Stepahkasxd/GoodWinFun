using System;

namespace GoodWin.Core
{
    /// <summary>
    /// Стадии игры для планирования дебаффов.
    /// </summary>
    public enum Stage
    {
        /// <summary>Начальная фаза без дебаффов (0–10 минут).</summary>
        Warmup,
        Easy,
        Medium,
        Hard
    }

    /// <summary>
    /// Планировщик дебаффов (Debuff Scheduler) с разделением по стадиям игры.
    /// </summary>
    public class DebuffScheduler
    {
        // Событие, возникающее при наступлении времени выбора нового дебаффа.
        public event EventHandler? DebuffSelectionPending;

        private readonly Random _random = new Random();

        /// <summary>
        /// Признаки включения стадий. Значения по умолчанию совпадают
        /// с предыдущим поведением планировщика, где все стадии активны.
        /// </summary>
        public bool EasyEnabled { get; private set; } = true;
        public bool MediumEnabled { get; private set; } = true;
        public bool HardEnabled { get; private set; } = true;

        // Текущая стадия (фаза) игры.
        private Stage _currentStage = Stage.Warmup;
        public Stage CurrentStage => _currentStage;

        // Флаг ожидания подтверждения (паузы планировщика до вызова Allow()).
        private bool _waitingForAllow = false;

        // Количество оставшихся срабатываний дебаффов в текущей стадии (для Easy/Medium).
        private int _triggersRemaining = 0;

        // Время следующего запланированного дебаффа (в игровых секундах).
        private double _nextTriggerTime = double.PositiveInfinity;

        // Последнее известное игровое время (для отслеживания прогресса времени и стадий).
        private double _lastGameTime = 0.0;

        // Параметры из конфигурации (Min/Max срабатываний для Easy/Medium и шанс для Hard).
        private readonly int _minSpinsEasy = 4;
        private readonly int _maxSpinsEasy = 10;
        private readonly int _minSpinsMedium = 7;
        private readonly int _maxSpinsMedium = 15;
        private readonly int _hardChancePercent = 19; // шанс (%), что дебафф сработает в Hard-стадии каждую секунду.

        // Пороговые значения времени для перехода стадий (в секундах).
        private readonly double _warmupStageDuration = 600.0;   // 0–10 мин (без дебаффов)
        private readonly double _easyStageDuration = 1200.0;    // 10–20 мин (Easy)
        private readonly double _mediumStageDuration = 1800.0;  // 20–30 мин (Medium)
        // Hard начинается после 30-й минуты (> 1800.0 секунд).

        public DebuffScheduler()
        {
            // Инициализация начальной стадии и планирование первого дебаффа.
            _currentStage = Stage.Warmup;
            _triggersRemaining = 0;
            _nextTriggerTime = double.PositiveInfinity;
        }

        /// <summary>
        /// Устанавливает, какие стадии игры разрешены. Вызывается из ViewModel
        /// при изменении состояния флажков в интерфейсе.
        /// </summary>
        public void SetEnabledStages(bool easy, bool medium, bool hard)
        {
            EasyEnabled = easy;
            MediumEnabled = medium;
            HardEnabled = hard;
        }

        /// <summary>
        /// Метод обновления планировщика, вызываемый при обновлении игрового времени.
        /// </summary>
        /// <param name="gameTime">Текущее игровое время в секундах.</param>
        public void Update(double gameTime)
        {
            // Если игровое время сбросилось (началась новая игра), выполнить полную переинициализацию.
            if (gameTime < _lastGameTime)
            {
                Reset(gameTime);
            }

            // Определяем текущую стадию игры исходя из времени.
            Stage newStage = DetermineStage(gameTime);
            if (newStage != _currentStage)
            {
                // Переход на новую стадию игры.
                _currentStage = newStage;
                // Сброс флага ожидания при смене стадии (начинаем новую фазу).
                _waitingForAllow = false;

                if (!IsStageEnabled(_currentStage))
                {
                    _triggersRemaining = 0;
                    _nextTriggerTime = double.PositiveInfinity;
                }
                else if (_currentStage == Stage.Hard)
                {
                    // В Hard-стадии используется вероятностный подход, фиксированное количество срабатываний не задаётся.
                    _triggersRemaining = 0;
                    _nextTriggerTime = double.PositiveInfinity;
                }
                else
                {
                    // Для стадий Easy/Medium задаём новое количество дебаффов и время следующего срабатывания.
                    _triggersRemaining = GetInitialTriggerCount(_currentStage);
                    _nextTriggerTime = (_triggersRemaining > 0)
                        ? CalculateNextTriggerTime(gameTime, _currentStage, _triggersRemaining)
                        : double.PositiveInfinity;
                }
            }

            // Обновляем сохранённое время (для вычислений шанс/интервалов).
            int lastSec = (int)Math.Floor(_lastGameTime);
            _lastGameTime = gameTime;

            // Если сейчас ожидается подтверждение предыдущего дебаффа, не планируем новые.
            if (_waitingForAllow)
                return;

            // В зависимости от текущей стадии игры проверяем, не пора ли запускать новый дебафф.
            if (_currentStage == Stage.Hard && IsStageEnabled(Stage.Hard))
            {
                // Для Hard-стадии: 19% шанс каждую игровую секунду.
                int currentSec = (int)Math.Floor(gameTime);
                if (currentSec > lastSec)
                {
                    // Проверяем каждую пропущенную целую секунду между lastSec и currentSec.
                    for (int sec = lastSec + 1; sec <= currentSec; sec++)
                    {
                        if (_random.Next(100) < _hardChancePercent)
                        {
                            // Сработало событие запуска дебаффа.
                            DebuffSelectionPending?.Invoke(this, EventArgs.Empty);
                            _waitingForAllow = true;
                            // Останавливаем дальнейшие проверки до Allow().
                            break;
                        }
                    }
                }
            }
            else if (IsStageEnabled(_currentStage))
            {
                // Для Easy/Medium стадий: планирование по запланированному времени и счётчику оставшихся дебаффов.
                if (_triggersRemaining > 0 && gameTime >= _nextTriggerTime)
                {
                    // Наступило запланированное время следующего дебаффа.
                    DebuffSelectionPending?.Invoke(this, EventArgs.Empty);
                    _waitingForAllow = true;
                    _triggersRemaining--;
                }
            }
        }

        /// <summary>
        /// Разрешает планировщику продолжить работу после того, как дебафф был выбран/применён.
        /// Вызывается, например, после завершения анимации рулетки или применения дебаффа.
        /// </summary>
        public void Allow()
        {
            if (!_waitingForAllow)
                return; // Если планировщик не находился в ожидании, ничего не делаем.

            // Снимаем паузу, позволяя планировщику продолжить работу.
            _waitingForAllow = false;

            if (_currentStage == Stage.Hard)
            {
                // В Hard-стадии следующий шанс проверяется на следующей секунде (реализация в Update).
                return;
            }

            // Для Easy/Medium: если остаются запланированные дебаффы, рассчитать время следующего.
            if (_triggersRemaining > 0)
            {
                _nextTriggerTime = CalculateNextTriggerTime(_lastGameTime, _currentStage, _triggersRemaining);
            }
            else
            {
                // Если в текущей стадии больше не осталось дебаффов, новых событий до смены стадии не будет.
                _nextTriggerTime = double.PositiveInfinity;
            }
        }

        /// <summary>
        /// Полный сброс планировщика (например, при новой игре).
        /// </summary>
        private void Reset(double gameTime = 0.0)
        {
            _currentStage = Stage.Warmup;
            _waitingForAllow = false;
            _lastGameTime = gameTime;
            _triggersRemaining = 0;
            _nextTriggerTime = double.PositiveInfinity;
        }

        /// <summary>
        /// Определяет стадию игры (Warmup, Easy, Medium или Hard) по текущему времени.
        /// </summary>
        private Stage DetermineStage(double gameTime)
        {
            if (gameTime >= _mediumStageDuration)
                return Stage.Hard;
            if (gameTime >= _easyStageDuration)
                return Stage.Medium;
            if (gameTime >= _warmupStageDuration)
                return Stage.Easy;
            return Stage.Warmup;
        }

        /// <summary>
        /// Проверяет, включена ли указанная стадия.
        /// </summary>
        private bool IsStageEnabled(Stage stage) => stage switch
        {
            Stage.Warmup => false,
            Stage.Easy => EasyEnabled,
            Stage.Medium => MediumEnabled,
            Stage.Hard => HardEnabled,
            _ => true
        };

        /// <summary>
        /// Вычисляет псевдослучайное количество дебаффов для новой стадии (Easy или Medium).
        /// </summary>
        private int GetInitialTriggerCount(Stage stage)
        {
            switch (stage)
            {
                case Stage.Easy:
                    return _random.Next(_minSpinsEasy, _maxSpinsEasy + 1);
                case Stage.Medium:
                    return _random.Next(_minSpinsMedium, _maxSpinsMedium + 1);
                default:
                    // Для Hard возвращаем 0 (используется отдельная логика вероятности).
                    return 0;
            }
        }

        /// <summary>
        /// Рассчитывает время (игровое) следующего срабатывания дебаффа для заданной стадии.
        /// </summary>
        /// <param name="currentTime">Текущее игровое время (в секундах).</param>
        /// <param name="stage">Текущая стадия игры.</param>
        /// <param name="triggersRemaining">Сколько дебаффов осталось в этой стадии (включая предстоящий).</param>
        /// <returns>Игровое время следующего дебаффа.</returns>
        private double CalculateNextTriggerTime(double currentTime, Stage stage, int triggersRemaining)
        {
            // Определяем конец текущей стадии в секундах.
            double stageEndTime = stage switch
            {
                Stage.Easy => _easyStageDuration,
                Stage.Medium => _mediumStageDuration,
                Stage.Warmup => _warmupStageDuration,
                _ => double.PositiveInfinity
            };
            if (triggersRemaining <= 0 || stageEndTime == double.PositiveInfinity)
            {
                // Защита: не рассчитуем если нет оставшихся дебаффов или стадия Hard (бесконечная).
                return double.PositiveInfinity;
            }

            // Оставшееся время текущей стадии.
            double timeLeft = stageEndTime - currentTime;
            if (timeLeft <= 0)
            {
                return stageEndTime; // стадия уже на исходе.
            }

            // Средний интервал между оставшимися дебаффами.
            double avgInterval = timeLeft / triggersRemaining;
            // Вводим случайность в интервал (от 50% до 150% среднего).
            double factor = 0.5 + _random.NextDouble(); // случайное число [0.5, 1.5)
            double interval = avgInterval * factor;
            // Рассчитываем время следующего срабатывания.
            double nextTime = currentTime + interval;
            // Гарантируем, что событие произойдёт до конца стадии.
            if (nextTime >= stageEndTime)
            {
                nextTime = stageEndTime - 0.1;
            }
            // Минимальный интервал - 1 секунда.
            if (nextTime < currentTime + 1)
            {
                nextTime = currentTime + 1;
            }
            return nextTime;
        }
    }
}
