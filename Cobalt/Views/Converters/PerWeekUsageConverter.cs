﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Data;
using Cobalt.Common.Analysis.OutputTypes;
using Cobalt.Common.Data;
using Cobalt.Common.UI.Util;
using Cobalt.Common.UI.ViewModels;
using Cobalt.Common.Util;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Helpers;
using LiveCharts.Wpf;

namespace Cobalt.Views.Converters
{
    public class PerWeekUsageConverter : IValueConverter
    {
        private static IEqualityComparer<App> PathEquality { get; }
            = new SelectorEqualityComparer<App, string>(a => a.Path);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var mapper = Mappers
                .Xy<AppDurationViewModel>()
                .Y(x => x.Duration.Ticks);
            var series = new SeriesCollection(mapper);
            if (!(value is IObservable<Usage<(App App, DateTime StartDay, TimeSpan Duration)>> coll)) return null;

            var appMap = new Dictionary<App, StackedColumnSeries>(PathEquality);

            //TODO RESOLVE DURATIONTIMER in a better way
            //var incrementor = IoCService.Instance.Resolve<IDurationIncrementor>();

            coll.ObserveOnDispatcher().Subscribe(ux =>
            {
                var x = ux.Value;
                //var justStarted = ux.JustStarted;
                if (!appMap.ContainsKey(x.App))
                {
                    var stack = new StackedColumnSeries
                    {
                        Values = new AppDurationViewModel[7].Select(_ => new AppDurationViewModel(x.App))
                            .AsChartValues(),
                        LabelPoint = cp => x.App.Path
                    };
                    appMap[x.App] = stack;
                    series.Add(stack);
                }

                var chunk = ((ChartValues<AppDurationViewModel>) appMap[x.App].Values)[(int)x.StartDay.DayOfWeek];
                chunk.Duration += x.Duration;
                //chunk.DurationIncrement(new Usage<TimeSpan>(justStarted:justStarted, value: x.Duration), incrementor);
            });


            return series;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        //TODO refactor this common componenent
        private void HandleDuration(Usage<TimeSpan> d, IDurationIncrementor incrementor, IHasDuration hasDur)
        {
            if (d.JustStarted)
            {
                //handle new app/tag started here
                incrementor.Increment(hasDur);
            }
            else
            {
                incrementor.Release();
                hasDur.Duration += d.Value;
            }
        }
    }
}