using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using LiveChartsCore;
using LiveChartsCore.Drawing;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Pulsar.Server.Statistics;
using SkiaSharp;

#nullable enable

namespace Pulsar.Server.Controls.Wpf
{
    public sealed class StatsViewModel : INotifyPropertyChanged
    {
        private static readonly SKColor[] LightPalette =
        {
            SKColor.Parse("#1976D2"),
            SKColor.Parse("#388E3C"),
            SKColor.Parse("#F57C00"),
            SKColor.Parse("#7B1FA2"),
            SKColor.Parse("#C2185B"),
            SKColor.Parse("#0097A7"),
            SKColor.Parse("#AFB42B")
        };

        private static readonly SKColor[] DarkPalette =
        {
            SKColor.Parse("#64B5F6"),
            SKColor.Parse("#81C784"),
            SKColor.Parse("#FFB74D"),
            SKColor.Parse("#BA68C8"),
            SKColor.Parse("#F06292"),
            SKColor.Parse("#4DD0E1"),
            SKColor.Parse("#DCE775")
        };

        private readonly ObservableCollection<StatCardViewModel> _statCards = new()
        {
            new StatCardViewModel("Total Clients"),
            new StatCardViewModel("Online Now"),
            new StatCardViewModel("Offline"),
            new StatCardViewModel("New (7 days)")
        };

        private ISeries[] _newClientsSeries = Array.Empty<ISeries>();
        private Axis[] _newClientsXAxes = Array.Empty<Axis>();
        private Axis[] _newClientsYAxes = Array.Empty<Axis>();
        private ISeries[] _clientsByCountrySeries = Array.Empty<ISeries>();
        private ISeries[] _clientsByOsSeries = Array.Empty<ISeries>();
        private bool _isLoading;
        private bool _hasError;
        private string? _errorMessage;
        private string _lastUpdated = "";
        private bool _isDarkMode;
        private ClientStatisticsSnapshot? _lastSnapshot;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ReadOnlyObservableCollection<StatCardViewModel> StatCards { get; }

        public ISeries[] NewClientsSeries
        {
            get => _newClientsSeries;
            private set => SetField(ref _newClientsSeries, value);
        }

        public Axis[] NewClientsXAxes
        {
            get => _newClientsXAxes;
            private set => SetField(ref _newClientsXAxes, value);
        }

        public Axis[] NewClientsYAxes
        {
            get => _newClientsYAxes;
            private set => SetField(ref _newClientsYAxes, value);
        }

        public ISeries[] ClientsByCountrySeries
        {
            get => _clientsByCountrySeries;
            private set => SetField(ref _clientsByCountrySeries, value);
        }

        public ISeries[] ClientsByOperatingSystemSeries
        {
            get => _clientsByOsSeries;
            private set => SetField(ref _clientsByOsSeries, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (SetField(ref _isLoading, value))
                {
                    OnPropertyChanged(nameof(IsContentVisible));
                }
            }
        }

        public bool HasError
        {
            get => _hasError;
            private set
            {
                if (SetField(ref _hasError, value))
                {
                    OnPropertyChanged(nameof(IsContentVisible));
                }
            }
        }

        public bool IsContentVisible => !IsLoading && !HasError;

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set => SetField(ref _errorMessage, value);
        }

        public string LastUpdated
        {
            get => _lastUpdated;
            private set => SetField(ref _lastUpdated, value);
        }

        public StatsViewModel()
        {
            StatCards = new ReadOnlyObservableCollection<StatCardViewModel>(_statCards);
        }

        public void SetLoading()
        {
            ErrorMessage = null;
            HasError = false;
            IsLoading = true;
        }

        public void SetError(string message)
        {
            _lastSnapshot = null;
            ErrorMessage = message;
            HasError = true;
            IsLoading = false;
            LastUpdated = string.Empty;
            ClearSeries();
        }

        public void UpdateSnapshot(ClientStatisticsSnapshot snapshot)
        {
            _lastSnapshot = snapshot;
            ErrorMessage = snapshot.ErrorMessage;
            HasError = snapshot.HasError;
            IsLoading = false;

            if (snapshot.HasError)
            {
                LastUpdated = string.Empty;
                ClearSeries();
                return;
            }

            LastUpdated = $"Updated {snapshot.GeneratedAtUtc.ToLocalTime():g}";
            UpdateCards(snapshot);
            BuildSeries();
        }

        public void UpdateTheme(bool isDarkMode)
        {
            _isDarkMode = isDarkMode;
            BuildSeries();
        }

        private void UpdateCards(ClientStatisticsSnapshot snapshot)
        {
            _statCards[0].Update(snapshot.TotalClients.ToString("N0"), "Unique clients recorded");
            _statCards[1].Update(snapshot.OnlineClients.ToString("N0"), "Currently connected");
            _statCards[2].Update(snapshot.OfflineClients.ToString("N0"), "Seen but offline");
            _statCards[3].Update(snapshot.NewClientsLast7Days.ToString("N0"), "Joined in last 7 days");
        }

        private void BuildSeries()
        {
            if (_lastSnapshot == null || _lastSnapshot.HasError)
            {
                ClearSeries();
                return;
            }

            var accent = GetAccentColor();
            var axisText = GetAxisTextColor();
            var separator = GetSeparatorColor();

            var dailyValues = _lastSnapshot.NewClientsByDay.Select(d => d.Count).ToArray();
            var labels = _lastSnapshot.NewClientsByDay.Select(d => d.Date.ToString("MMM dd")).ToArray();

            NewClientsSeries = new ISeries[]
            {
                CreateColumnSeries(dailyValues, accent, axisText)
            };

            NewClientsXAxes = new[]
            {
                new Axis
                {
                    Labels = labels,
                    LabelsPaint = new SolidColorPaint(axisText),
                    Name = "Day",
                    NamePaint = new SolidColorPaint(axisText),
                    TextSize = 13,
                    Padding = new Padding(10, 0, 10, 0),
                    SeparatorsPaint = new SolidColorPaint(separator) { StrokeThickness = 1 }
                }
            };

            NewClientsYAxes = new[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(axisText),
                    TextSize = 13,
                    Name = "Clients",
                    NamePaint = new SolidColorPaint(axisText),
                    MinLimit = 0,
                    SeparatorsPaint = new SolidColorPaint(separator) { StrokeThickness = 1 }
                }
            };

            ClientsByCountrySeries = BuildPieSeries(_lastSnapshot.ClientsByCountry);
            ClientsByOperatingSystemSeries = BuildPieSeries(_lastSnapshot.ClientsByOperatingSystem);
        }

        private ISeries[] BuildPieSeries(IReadOnlyCollection<CategoryCount> categories)
        {
            if (categories == null || categories.Count == 0)
            {
                return Array.Empty<ISeries>();
            }

            var palette = _isDarkMode ? DarkPalette : LightPalette;
            var axisText = GetAxisTextColor();
            var stroke = GetSeparatorColor();

            var series = categories
                .Select((entry, index) =>
                {
                    var pieSeries = new PieSeries<int>
                    {
                        Values = new[] { entry.Count },
                        Name = entry.Label,
                        Fill = new SolidColorPaint(palette[index % palette.Length]),
                        Stroke = new SolidColorPaint(stroke) { StrokeThickness = 1.5f },
                        DataLabelsPaint = new SolidColorPaint(axisText),
                        DataLabelsSize = 12,
                        DataLabelsPosition = PolarLabelsPosition.Middle,
                        DataLabelsFormatter = point =>
                        {
                            var value = point.Model;
                            return value > 0
                                ? $"{entry.Label}: {value:N0} ({entry.Share:P1})"
                                : entry.Label;
                        }
                    };

                    return pieSeries;
                })
                .Cast<ISeries>()
                .ToArray();

            return series;
        }

        private static ColumnSeries<int> CreateColumnSeries(int[] values, SKColor accent, SKColor axisText)
        {
            var series = new ColumnSeries<int>
            {
                Values = values,
                Fill = new SolidColorPaint(accent),
                Stroke = null
            };

            if (values.Length <= 10 && values.Any(v => v > 0))
            {
                series.DataLabelsPaint = new SolidColorPaint(axisText);
                series.DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top;
                series.DataLabelsFormatter = point => point.Model.ToString("N0");
            }

            return series;
        }

        private void ClearSeries()
        {
            NewClientsSeries = Array.Empty<ISeries>();
            NewClientsXAxes = Array.Empty<Axis>();
            NewClientsYAxes = Array.Empty<Axis>();
            ClientsByCountrySeries = Array.Empty<ISeries>();
            ClientsByOperatingSystemSeries = Array.Empty<ISeries>();
        }

        private SKColor GetAccentColor() => _isDarkMode ? SKColor.Parse("#64B5F6") : SKColor.Parse("#1E88E5");

        private SKColor GetAxisTextColor() => _isDarkMode ? SKColors.White : SKColor.Parse("#1A1A1A");

        private SKColor GetSeparatorColor() => _isDarkMode ? SKColor.Parse("#424242") : SKColor.Parse("#BDBDBD");

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public sealed class StatCardViewModel : INotifyPropertyChanged
    {
        private string _value = "0";
        private string _subtitle = string.Empty;

        public StatCardViewModel(string title)
        {
            Title = title;
        }

        public string Title { get; }

        public string Value
        {
            get => _value;
            private set => SetField(ref _value, value);
        }

        public string Subtitle
        {
            get => _subtitle;
            private set => SetField(ref _subtitle, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Update(string value, string subtitle)
        {
            Value = value;
            Subtitle = subtitle;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

}
