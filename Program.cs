using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace observer_strategy
{
    // ==================== Паттерн Наблюдатель (Observer) ====================
    public class MetricData
    {
        public string MetricName { get; }
        public double Value { get; }
        public double Threshold { get; }
        public DateTime Timestamp { get; }

        public MetricData(string metricName, double value, double threshold, DateTime timestamp)
        {
            MetricName = metricName ?? throw new ArgumentNullException(nameof(metricName));
            Value = value;
            Threshold = threshold;
            Timestamp = timestamp;
        }

        public override string ToString()
        {
            return $"Metric: {MetricName}, Value: {Value} (Threshold: {Threshold})";
        }
    }

    /// <summary>
    /// Аргументы события
    /// </summary>
    public class MetricEventArgs : EventArgs
    {
        public string EventType { get; }
        public MetricData Data { get; }

        public MetricEventArgs(string eventType, MetricData data)
        {
            EventType = eventType;
            Data = data;
        }
    }

    // Делегат
    public delegate void MetricEventHandler(MetricEventArgs e);

    /// <summary>
    /// Субъект (Издатель)
    /// </summary>
    public class EventMonitor
    {
        public event MetricEventHandler? OnMetricExceeded;

        public void CheckMetric(string metricName, double value, double threshold)
        {
            Console.WriteLine($"[Monitor]: Checking {metricName} ({value} vs {threshold})");
            if (value > threshold)
            {
                var eventData = new MetricData(metricName, value, threshold, DateTime.Now);
                OnMetricExceeded?.Invoke(new MetricEventArgs(metricName + "_Exceeded", eventData));
            }
        }
    }

    // ==================== Паттерн Стратегия (Strategy) ====================

    /// <summary>
    /// Интерфейс стратегии форматирования
    /// </summary>
    public interface IFormatStrategy
    {
        string Format(string message, DateTime timestamp);
    }

    public class TextFormatStrategy : IFormatStrategy
    {
        public string Format(string message, DateTime timestamp)
        {
            return $"[{timestamp:HH:mm:ss}] TEXT: {message}";
        }
    }

    public class JsonFormatStrategy : IFormatStrategy
    {
        public string Format(string message, DateTime timestamp)
        {
            return $"{{\"timestamp\":\"{timestamp:HH:mm:ss}\",\"format\":\"JSON\",\"message\":\"{message}\"}}";
        }
    }

    public class HtmlFormatStrategy : IFormatStrategy
    {
        public string Format(string message, DateTime timestamp)
        {
            return $"<div><b>[{timestamp:HH:mm:ss}]</b> <i>HTML:</i> {message}</div>";
        }
    }

    // ==================== Паттерн Шаблонный метод (Template Method) ====================

    /// <summary>
    /// Абстрактный базовый класс
    /// </summary>
    public abstract class EventHandlerBase
    {
        protected IFormatStrategy _formatStrategy;

        protected EventHandlerBase(IFormatStrategy strategy)
        {
            _formatStrategy = strategy;
        }

        public void SetStrategy(IFormatStrategy strategy)
        {
            _formatStrategy = strategy;
        }

        // Шаблонный метод
        public void ProcessEvent(MetricEventArgs e)
        {
            var message = FormatMessage(e.EventType, e.Data);
            SendMessage(message);
            LogResult();
        }

        protected abstract string FormatMessage(string type, object data);
        protected abstract void SendMessage(string message);
        protected virtual void LogResult()
        {
            Console.WriteLine("[Handler]: Logged result");
        }
    }

    /// <summary>
    /// Конкретный класс - вывод в консоль
    /// </summary>
    public class ConsoleHandler : EventHandlerBase
    {
        public ConsoleHandler(IFormatStrategy strategy) : base(strategy) { }

        protected override string FormatMessage(string type, object data)
        {
            var metricData = data as MetricData;
            string content = $"[{type}] {metricData}";
            return _formatStrategy.Format(content, DateTime.Now);
        }

        protected override void SendMessage(string message)
        {
            Console.WriteLine($"[ConsoleHandler]: {message}");
        }
    }

    /// <summary>
    /// Конкретный класс - запись в файл
    /// </summary>
    public class FileHandler : EventHandlerBase
    {
        private readonly string _filePath = "events.log";

        public FileHandler(IFormatStrategy strategy) : base(strategy) { }

        protected override string FormatMessage(string type, object data)
        {
            var metricData = data as MetricData;
            string content = $"[{type}] {metricData}";
            return _formatStrategy.Format(content, DateTime.Now);
        }

        protected override void SendMessage(string message)
        {
            File.AppendAllText(_filePath, message + Environment.NewLine);
            Console.WriteLine($"[FileHandler]: Written to {_filePath}");
        }
    }

    // ==================== Демонстрация ====================

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Лабораторная работа №7 ===\n");

            // Создаём монитор событий (издатель)
            var monitor = new EventMonitor();

            // Создаём обработчики с разными стратегиями форматирования
            var consoleText = new ConsoleHandler(new TextFormatStrategy());
            var consoleJson = new ConsoleHandler(new JsonFormatStrategy());
            var fileHtml = new FileHandler(new HtmlFormatStrategy());

            // Подписываемся на события
            monitor.OnMetricExceeded += consoleText.ProcessEvent;
            monitor.OnMetricExceeded += consoleJson.ProcessEvent;
            monitor.OnMetricExceeded += fileHtml.ProcessEvent;

            // Демонстрация смены стратегии во время выполнения
            Console.WriteLine("=== Демонстрация смены стратегии ===");
            Console.WriteLine("Текущая стратегия consoleJson: JSON");
            consoleJson.SetStrategy(new TextFormatStrategy());
            Console.WriteLine("Сменили стратегию consoleJson на TEXT\n");

            // Моделируем события
            Console.WriteLine("=== Моделирование событий мониторинга ===\n");

            monitor.CheckMetric("CPU_Load", 85.0, 80.0);
            Thread.Sleep(500);

            monitor.CheckMetric("Memory_Usage", 95.0, 90.0);
            Thread.Sleep(500);

            monitor.CheckMetric("Network_Traffic", 120.0, 100.0);

            Console.WriteLine("\n=== Работа завершена ===");
        }
    }
}