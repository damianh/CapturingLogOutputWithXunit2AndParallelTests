﻿namespace Lib.Tests
{
    using System;
    using System.IO;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using Serilog;
    using Serilog.Context;
    using Serilog.Events;
    using Xunit.Abstractions;

    internal static class LoggingHelper
    {
        private static readonly Subject<LogEvent>  s_logEventSubject = new Subject<LogEvent>();
        private const string CaptureIdKey = "captureid";

        static LoggingHelper()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo
                //Could this be nicer.
                .Observers(observable => observable.Subscribe(logEvent => s_logEventSubject.OnNext(logEvent)))
                .Enrich.FromLogContext()
                .CreateLogger();
        }

        public static IDisposable Capture(ITestOutputHelper testOutputHelper)
        {
            var captureId = Guid.NewGuid();

            Func<LogEvent, bool> filter = logEvent => 
                logEvent.Properties.ContainsKey(CaptureIdKey) &&
                logEvent.Properties[CaptureIdKey].ToString() == captureId.ToString();

            var subscription = s_logEventSubject.Where(filter).Subscribe(logEvent =>
            {

                //TODO nicer way to do the?
                using(var writer = new StringWriter())
                {
                    logEvent.RenderMessage(writer);
                    testOutputHelper.WriteLine(writer.ToString());
                }
            });
            var pushProperty = LogContext.PushProperty(CaptureIdKey, captureId);

            return new DisposableAction(() =>
            {
                subscription.Dispose();
                pushProperty.Dispose();
            });
        }

        private class DisposableAction : IDisposable
        {
            private readonly Action _action;

            public DisposableAction(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                _action();
            }
        }
    }
}