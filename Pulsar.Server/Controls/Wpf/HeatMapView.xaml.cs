using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using LiveChartsCore.Geo;
using LiveChartsCore.SkiaSharpView.WPF;
using Pulsar.Server.Statistics;

#nullable enable

namespace Pulsar.Server.Controls.Wpf
{
    public partial class HeatMapView : UserControl
    {
        private readonly HeatMapViewModel _viewModel;
        private readonly GeoMap _geoMap;

        public HeatMapView()
        {
            InitializeComponent();
            _viewModel = new HeatMapViewModel();
            DataContext = _viewModel;

            _geoMap = CreateGeoMap();
            MapHost.Content = _geoMap;

            Bind(_geoMap, GeoMap.SeriesProperty, nameof(HeatMapViewModel.Series));
        }

        public void ShowLoading()
        {
            Dispatcher.Invoke(_viewModel.SetLoading);
        }

        public void ShowError(string message)
        {
            Dispatcher.Invoke(() => _viewModel.SetError(message));
        }

        public void UpdateSnapshot(ClientGeoSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            Dispatcher.Invoke(() => _viewModel.UpdateSnapshot(snapshot));
        }

        public void ApplyTheme(bool isDarkMode)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateBrush("StatsBackgroundBrush", isDarkMode ? "#FF1A1A1A" : "#FFFFFFFF");
                UpdateBrush("CardBackgroundBrush", isDarkMode ? "#FF222327" : "#FFF5F5F5");
                UpdateBrush("CardBorderBrush", isDarkMode ? "#FF2E3136" : "#FFE0E0E0");
                UpdateBrush("CardForegroundBrush", isDarkMode ? "#FFE8EAED" : "#FF1F1F1F");
                UpdateBrush("MutedTextBrush", isDarkMode ? "#FF9AA0A6" : "#FF5F6368");
                UpdateBrush("AccentBrush", isDarkMode ? "#FF64B5F6" : "#FF1976D2");
                UpdateBrush("SectionHeaderBrush", isDarkMode ? "#FF64B5F6" : "#FF1976D2");
                UpdateBrush("ChartBackgroundBrush", isDarkMode ? "#FF1E1F23" : "#FFFFFFFF");
                UpdateBrush("ChartBorderBrush", isDarkMode ? "#FF2F3338" : "#FFE0E0E0");
                UpdateBrush("ScrollBarTrackBrush", isDarkMode ? "#FF1E1E1E" : "#FFE5E5E5");
                UpdateBrush("ScrollBarThumbBrush", isDarkMode ? "#FF444444" : "#FFB5B5B5");
                UpdateBrush("ScrollBarThumbHoverBrush", isDarkMode ? "#FF5A5A5A" : "#FF9E9E9E");
                UpdateBrush("ScrollBarThumbPressedBrush", isDarkMode ? "#FF737373" : "#FF7C7C7C");

                LayoutRoot.Background = (Brush)Resources["StatsBackgroundBrush"];
                ApplyMapTheme();
                _viewModel.UpdateTheme(isDarkMode);
            });
        }

        private void UpdateBrush(string resourceKey, string hex)
        {
            var color = (Color)ColorConverter.ConvertFromString(hex)!;
            if (Resources[resourceKey] is SolidColorBrush brush)
            {
                if (!brush.IsFrozen)
                {
                    brush.Color = color;
                }
                else
                {
                    var mutable = brush.Clone();
                    mutable.Color = color;
                    Resources[resourceKey] = mutable;
                }
            }
            else
            {
                Resources[resourceKey] = new SolidColorBrush(color);
            }
        }

        private void ApplyMapTheme()
        {
            if (Resources["ChartBackgroundBrush"] is not SolidColorBrush chartBackground)
            {
                return;
            }

            if (Resources["ChartBorderBrush"] is not SolidColorBrush chartBorder)
            {
                return;
            }

            _geoMap.Background = chartBackground;
            _geoMap.BorderBrush = chartBorder;
            _geoMap.BorderThickness = new Thickness(1);
        }

        private static GeoMap CreateGeoMap()
        {
            return new GeoMap
            {
                Height = 360,
                Padding = new Thickness(8)
            };
        }

        private static Binding CreateOneWayBinding(string path)
        {
            return new Binding(path)
            {
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
        }

        private static void Bind(FrameworkElement element, DependencyProperty property, string path)
        {
            element.SetBinding(property, CreateOneWayBinding(path));
        }
    }
}
