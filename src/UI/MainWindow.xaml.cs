using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Timers = System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;

namespace TP_Link.Kasa.Bulb.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly Style style;
        private static readonly int requestTimeout = 5000;
        private readonly TPLink.TPLink tpLink = new TPLink.TPLink();
        private XAMLStyles xamlStyles = new XAMLStyles();
        private TPLink.SDevice? activeDevice = null;
        private (ListBoxItem, int, List<TPLink.SLightState>, CancellationTokenSource) activeScript = (null, -1, null, null);

        public MainWindow()
        {
            InitializeComponent();

            //Create data binding.
            DataContext = xamlStyles;
            Styles.themesChanged += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    //This data context reset isn't working and I'm not sure why.
                    xamlStyles.backgroundSolid = Styles.background;
                    xamlStyles.foregroundSolid = Styles.foreground;
                    xamlStyles.accentSolid = Styles.accent;
                    xamlStyles.borderSolid = Styles.border;
                    DataContext = xamlStyles.CloneViaJson();
                });
            };

            style = (Style)FindResource("ListBoxItemStyle1");

            //Load AppData, if there is any.
            AppData.Load();
            if (AppData.data.email != null && AppData.data.password != null)
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                cts.CancelAfter(requestTimeout);
                tpLink.LogIn(AppData.data.email, AppData.data.password, cts.Token).ContinueWith(async (t) =>
                {
                    if (t.IsFaulted) return;
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        email.Text = AppData.data.email;
                        password.Password = AppData.data.password;
                        await LoadDevices();
                    });
                });
            }

            //Load scripts.
            Scripts.LoadScriptsInfo();
            deleteScriptButton.Visibility = Visibility.Collapsed;
            runOrStopScriptButton.Visibility = Visibility.Collapsed;
            deleteFrameButton.Visibility = Visibility.Collapsed;
            scriptFrames.IsEnabled = false;
            scriptFrameOptions.IsEnabled = false;
            foreach (string scriptName in Scripts.scripts)
            {
                ListBoxItem item = new ListBoxItem();
                item.Content = scriptName;
                item.Tag = scriptName;
                item.MouseDoubleClick += (s, e) => { SetActiveScript(item); };
                item.Style = style;
                scriptsList.Items.Add(item);
            }
        }

        private async void logIn_Click(object sender, RoutedEventArgs e)
        {
            loginGroup.IsEnabled = false;
            ClearDevices();

            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(requestTimeout);

            string _email = email.Text;
            string _password = password.Password;

            bool isFaulted = true;
            try
            {
                await tpLink.LogIn(_email, _password, cts.Token);
                isFaulted = false;
            }
            catch{}
            cts.Dispose();

            Brush brush;
            if (isFaulted) brush = Brushes.Red;
            else
            {
                brush = Brushes.LimeGreen;
                AppData.data.email = _email;
                AppData.data.password = _password;
                AppData.Save();
            }

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                loginGroup.BorderBrush = brush;
                loginGroup.IsEnabled = true;
            });

            Timers.Timer timer = new Timers.Timer(2500);
            timer.AutoReset = false;
            timer.Elapsed += async (s, _e) =>
            {
                await Application.Current.Dispatcher.InvokeAsync(() => loginGroup.BorderBrush = Brushes.Gray);
                timer.Dispose();
            };
            timer.Start();

            await LoadDevices();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.HideWindowFromTabManager();
        }

        private void minimisebtn_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void MenuBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void ClearDevices()
        {
            activeDevice = null;
            Dispatcher.Invoke(() =>
            {
                devicesList.Items.Clear();
                deviceOptions.IsEnabled = false;
                deviceOptions.Header = "Device Options";
                warmthSlider.Value = 2500;
                hueSlider.Value = 360;
                saturationSlider.Value = 100;
                brightnessSlider.Value = 100;

                xamlStyles.bulbColour = "#FFD9D9D9";
                DataContext = xamlStyles.CloneViaJson();
            });
        }

        private async Task LoadDevices()
        {
            ClearDevices();
            IEnumerable<TPLink.SDevice> devices = await tpLink.GetDevices();
            Dictionary<string, int> duplicateDeviceAliases = new Dictionary<string, int>();
            foreach (TPLink.SDevice device in devices)
            {
                string uiName = device.alias;
                if (duplicateDeviceAliases.ContainsKey(device.alias)) uiName += $" {++duplicateDeviceAliases[device.alias]}";
                else if (devices.GroupBy(d => d.alias).Max(g => g.Count()) > 1)
                {
                    //Check if there is going to be at least one more occurance of this alias, if there will be, add it to the dictionary.
                    duplicateDeviceAliases.Add(device.alias, 1);
                    uiName += $" {duplicateDeviceAliases[device.alias]}";
                }

                ListBoxItem item = new ListBoxItem();
                item.Content = device.alias;
                item.Tag = device.hwId;
                item.MouseDoubleClick += (s, e) => { Bulb_MouseDoubleClick(device, uiName); };
                item.Style = style;
                devicesList.Items.Add(item);
            }
        }

        private async void Bulb_MouseDoubleClick(TPLink.SDevice device, string uiName)
        {
            TPLink.SDeviceInfo deviceInfo;
            try { deviceInfo = await tpLink.GetDeviceInfo(device.deviceId); }
            catch (Exception ex)
            {
                if (ex.Message == "Device is offline")
                {
                    MessageBox.Show($"Device '{device.alias}' is offline.", "TP-Link Kasa Bulb.", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                else throw new Exception("Failed to get device info", ex);
            }
            activeDevice = device;
            await Dispatcher.InvokeAsync(() =>
            {
                deviceOptions.IsEnabled = true;
                deviceOptions.Header = $"Device Options - {uiName}";
                if (deviceInfo.system.getSYSInfo.lightState.onOff == 0)
                {
                    warmthSlider.Value = 2500;
                    hueSlider.Value = 360;
                    saturationSlider.Value = 100;
                    brightnessSlider.Value = 0;
                    colourSliders.IsEnabled = false;
                    scriptsContainer.IsEnabled = false;
                    modeOff.IsChecked = true;
                }
                else if (deviceInfo.system.getSYSInfo.lightState.colorTemp != 0)
                {
                    warmthSlider.Value = deviceInfo.system.getSYSInfo.lightState.colorTemp;
                    hueSlider.Value = 360;
                    saturationSlider.Value = 100;
                    brightnessSlider.Value = deviceInfo.system.getSYSInfo.lightState.brightness;
                    modeWhite.IsChecked = true;
                }
                else
                {
                    warmthSlider.Value = 2500;
                    hueSlider.Value = deviceInfo.system.getSYSInfo.lightState.hue;
                    saturationSlider.Value = deviceInfo.system.getSYSInfo.lightState.saturation;
                    brightnessSlider.Value = deviceInfo.system.getSYSInfo.lightState.brightness;
                    modeRGB.IsChecked = true;
                }
            });
        }

        //http://colorizer.org
        private void UpdateSliderThemes(bool useWarmthSlider)
        {
            Dispatcher.Invoke(() =>
            {
                if (
                    hueSlider == null
                    || saturationSlider == null
                    || brightnessSlider == null
                    || warmthSlider == null
                ) return;

                if (useWarmthSlider)
                {
                    //https://stackoverflow.com/questions/5953552/how-to-get-the-closest-number-from-a-listint-with-linq
                    KeyValuePair<int, (int, int, int)> closestMatch = TPLink.Data.kelvinToHsl.Aggregate((x, y) =>
                        Math.Abs(x.Key - warmthSlider.Value) < Math.Abs(y.Key - warmthSlider.Value) ? x : y);

                    string colour = Helpers.HslToHex(closestMatch.Value);
                    xamlStyles.hueHandleColour = colour;
                    xamlStyles.warmthHandleColour = colour;
                    xamlStyles.brightnessHandleColour = Helpers.HslToHex(closestMatch.Value.Item1, 100, brightnessSlider.Value / 2);
                    xamlStyles.bulbColour = Helpers.HslToHex(closestMatch.Value.Item1, 100,
                        Helpers.HsvToHsl(0, 100, brightnessSlider.Value).Item3);
                }
                else
                {
                    xamlStyles.hueHandleColour = Helpers.HslToHex(hueSlider.Value, 100, 50);
                    xamlStyles.saturationHandleColour = Helpers.HslToHex(hueSlider.Value, 100, 150 - (50 + (saturationSlider.Value / 2)));
                    xamlStyles.brightnessHandleColour = Helpers.HslToHex(hueSlider.Value, 100, brightnessSlider.Value / 2);
                    xamlStyles.bulbColour = Helpers.HslToHex(Helpers.HsvToHsl(hueSlider.Value, saturationSlider.Value, brightnessSlider.Value));
                }
                DataContext = xamlStyles.CloneViaJson();
            });
        }

        private void colourSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateSliderThemes(false);
        }

        private async void colourSlider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (activeDevice == null) return;
            TPLink.SDevice _activeDevice = (TPLink.SDevice)activeDevice;

            if (brightnessSlider.Value == 0) await tpLink.SetLightState(_activeDevice, false, 500);
            else
            {
                await tpLink.SetLightState(_activeDevice, true, 500, new TPLink.SPowerOptions
                {
                    hue = (int)hueSlider.Value,
                    saturation = (int)saturationSlider.Value,
                    brightness = (int)brightnessSlider.Value
                });
            }
        }

        private void warmthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateSliderThemes(true);
        }

        private async void warmthSlider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (activeDevice == null) return;
            TPLink.SDevice _activeDevice = (TPLink.SDevice)activeDevice;

            if (brightnessSlider.Value == 0) await tpLink.SetLightState(_activeDevice, false, 500);
            else
            {
                await tpLink.SetLightState(_activeDevice, true, 500, new TPLink.SPowerOptions
                {
                    colorTemp = (int)warmthSlider.Value,
                    brightness = (int)brightnessSlider.Value
                });
            }
        }

        private void brightnessSlider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (modeWhite.IsChecked == true) warmthSlider_DragCompleted(sender, e);
            else if (modeRGB.IsChecked == true || modeScript.IsChecked == true) colourSlider_DragCompleted(sender, e);
        }

        private void brightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (modeWhite.IsChecked == true) warmthSlider_ValueChanged(sender, e);
            else if (modeRGB.IsChecked == true || modeScript.IsChecked == true) colourSlider_ValueChanged(sender, e);
        }

        private async void modeOff_Checked(object sender, RoutedEventArgs e)
        {
            if (colourSliders == null) return;
            colourSliders.IsEnabled = false;
            scriptsContainer.IsEnabled = false;

            ClearActiveScript();
            if (activeDevice == null) return;
            await tpLink.SetLightState((TPLink.SDevice)activeDevice, false, 500);
        }

        private void modeWhite_Checked(object sender, RoutedEventArgs e)
        {
            if (activeDevice == null) return;
            colourSliders.IsEnabled = true;
            warmthSlider.IsEnabled = true;
            hueSlider.IsEnabled = false;
            saturationSlider.IsEnabled = false;
            scriptsContainer.IsEnabled = false;

            ClearActiveScript();
            UpdateSliderThemes(true);
            warmthSlider_DragCompleted(sender, default);
        }

        private void modeRGB_Checked(object sender, RoutedEventArgs e)
        {
            colourSliders.IsEnabled = true;
            warmthSlider.IsEnabled = false;
            hueSlider.IsEnabled = true;
            saturationSlider.IsEnabled = true;
            scriptsContainer.IsEnabled = false;

            ClearActiveScript();
            UpdateSliderThemes(false);
            colourSlider_DragCompleted(sender, default);
        }

        private void modeScript_Checked(object sender, RoutedEventArgs e)
        {
            colourSliders.IsEnabled = true;
            warmthSlider.IsEnabled = false;
            hueSlider.IsEnabled = true;
            saturationSlider.IsEnabled = true;
            scriptsContainer.IsEnabled = true;

            UpdateSliderThemes(false);
        }

        private void addOrUpdateScript_Click(object sender, RoutedEventArgs e)
        {
            if (activeScript.Item1 == null)
            {
                Scripts.AddScript(scriptName.Text, default);
                ListBoxItem item = new ListBoxItem();
                item.Content = scriptName.Text;
                item.Tag = scriptName.Text;
                item.MouseDoubleClick += (_s, _e) => { SetActiveScript(item); };
                item.Style = style;
                scriptsList.Items.Add(item);
                scriptsList.SelectedItem = item;
                activeScript = (item, -1, new List<TPLink.SLightState>(), null);
                SetActiveScript(item);
            }
            else
            {
                if (scriptName.Text != (string)activeScript.Item1.Tag)
                {
                    Scripts.UpdateScript((string)activeScript.Item1.Tag, activeScript.Item3, scriptName.Text);
                    activeScript.Item1.Content = scriptName.Text;
                    activeScript.Item1.Tag = scriptName.Text;
                }
                else Scripts.UpdateScript((string)activeScript.Item1.Tag, activeScript.Item3);
            }
        }

        private void SetActiveScript(ListBoxItem item)
        {
            List<TPLink.SLightState> frames = Scripts.LoadScript((string)item.Tag);

            scriptName.Text = (string)item.Tag;
            addOrUpdateScriptButton.Content = "Update";
            deleteScriptButton.Visibility = Visibility.Visible;
            runOrStopScriptButton.Visibility = Visibility.Visible;

            if (activeScript.Item4 != null) activeScript.Item4.Cancel();
            activeScript = (item, -1, frames, null);
            scriptFrames.Items.Clear();
            for (int i = 0; i < frames.Count; i++)
            {
                ListBoxItem frame = new ListBoxItem();
                frame.Tag = i;
                frame.Content = $"H:{frames[i].hue}*, S:{frames[i].saturation}%, B:{frames[i].brightness}, {frames[i].transitionPeriod}ms";
                frame.MouseDoubleClick += (_s, _e) => SetActiveFrame(frame);
                scriptFrames.Items.Add(frame);
            }
            scriptFrames.IsEnabled = true;
            scriptFrameOptions.IsEnabled = true;
        }

        private void deleteScript_Click(object sender, RoutedEventArgs e)
        {
            if (activeScript.Item1 == null) return;
            Scripts.RemoveScript((string)activeScript.Item1.Tag);
            scriptsList.Items.Remove(activeScript.Item1);
            ClearActiveScript();
        }

        private void scriptsList_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ClearActiveScript();
        }

        private void ClearActiveScript()
        {
            if (activeScript.Item4 != null) activeScript.Item4.Cancel();
            activeScript = (null, -1, null, null);

            addOrUpdateScriptButton.Content = "Add";
            addFrameButton.Content = "Add";
            scriptName.Text = "";
            deleteScriptButton.Visibility = Visibility.Collapsed;
            runOrStopScriptButton.Visibility = Visibility.Collapsed;

            scriptFrames.Items.Clear();
            scriptFrames.IsEnabled = false;
            scriptFrameOptions.IsEnabled = false;
        }

        private void runOrStopScript_click(object sender, RoutedEventArgs e)
        {
            if (activeScript.Item1 == null) return;
            else if (activeScript.Item4 != null)
            {
                //Stop script.
                runOrStopScriptButton.Content = "Run Script";
                activeScript.Item4.Cancel();
                activeScript.Item4 = null;
            }
            else if (activeScript.Item3.Count() > 0)
            {
                //Start script.
                runOrStopScriptButton.Content = "Stop Script";
                activeScript.Item4 = new CancellationTokenSource();

                scriptFrames.IsEnabled = false;
                scriptFrameOptions.IsEnabled = false;
                Task.Run(async () =>
                {
                    if (activeDevice == null || activeScript.Item3 == null) return;
                    TPLink.SDevice device = (TPLink.SDevice)activeDevice;
                    TPLink.SDeviceInfo deviceInfo = await tpLink.GetDeviceInfo(device.deviceId);
                    List<TPLink.SLightState> frames = activeScript.Item3;
                    System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

                    while (activeScript.Item4 != null && !activeScript.Item4.IsCancellationRequested)
                    {
                        for (int i = 0; i < frames.Count; i++)
                        {
                            if (activeScript.Item4 == null || activeScript.Item4.IsCancellationRequested) return;

                            TPLink.SLightState frame = frames[i];

                            stopwatch.Restart();
                            if (frame.brightness == 0) await tpLink.SetLightState(device, false, frame.transitionPeriod);
                            else
                            {
                                await tpLink.SetLightState(device, true, frame.transitionPeriod, new TPLink.SPowerOptions
                                {
                                    hue = frame.hue,
                                    saturation = frame.saturation,
                                    brightness = frame.brightness
                                });
                            }

                            await Dispatcher.InvokeAsync(() =>
                            {
                                hueSlider.Value = frame.hue;
                                saturationSlider.Value = frame.saturation;
                                brightnessSlider.Value = frame.brightness;
                            });
                            UpdateSliderThemes(false);

                            stopwatch.Stop();
                            if (stopwatch.Elapsed > TimeSpan.Zero && stopwatch.ElapsedMilliseconds < frame.transitionPeriod)
                                Thread.Sleep(TimeSpan.FromMilliseconds(frame.transitionPeriod - stopwatch.ElapsedMilliseconds));
                        }
                    }
                }, activeScript.Item4.Token).ContinueWith(t => Dispatcher.Invoke(() =>
                {
                    runOrStopScriptButton.Content = "Run Script";
                    scriptFrames.IsEnabled = true;
                    scriptFrameOptions.IsEnabled = true;
                }));
            }
        }

        private void addOrUpdateFrame_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(delayInput.Text, out int delay) || delay < 100 || delay > 60000)
            {
                MessageBox.Show("The specified frame delay is invalid.\nThe value must be a whole number, greater than 100 and less than 60,000.",
                    "TP-Link Kasa Bulb.", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            TPLink.SLightState frame = new TPLink.SLightState
            {
                hue = (int)hueSlider.Value,
                saturation = (int)saturationSlider.Value,
                brightness = (int)brightnessSlider.Value,
                transitionPeriod = delay
            };
            int index = activeScript.Item2 != -1 ? activeScript.Item2 : activeScript.Item3.Count();
            ListBoxItem item = new ListBoxItem();
            item.Content = $"H:{frame.hue}*, S:{frame.saturation}%, B:{frame.brightness}, {frame.transitionPeriod}ms";
            item.Tag = index;
            item.MouseDoubleClick += (_s, _e) => SetActiveFrame(item);
            item.Style = style;

            if (activeScript.Item2 == -1)
            {
                //Add frame.
                activeScript.Item3.Add(frame);
                scriptFrames.Items.Add(item);
                //SetActiveFrame(index);
            }
            else
            {
                //Update frame.
                activeScript.Item3.RemoveAt(activeScript.Item2);
                activeScript.Item3.Insert(activeScript.Item2, frame);
                scriptFrames.Items.RemoveAt(activeScript.Item2);
                scriptFrames.Items.Insert(activeScript.Item2, item);
            }
        }

        private async void SetActiveFrame(ListBoxItem index)
        {
            deleteFrameButton.Visibility = Visibility.Visible;
            addFrameButton.Content = "Update";

            if (activeDevice == null) return;
            TPLink.SDevice _activeDevice = (TPLink.SDevice)activeDevice;

            if (activeScript.Item1 == null) return;
            activeScript.Item2 = (int)index.Tag;
            TPLink.SLightState frame = activeScript.Item3[(int)index.Tag];

            await Dispatcher.InvokeAsync(() =>
            {
                hueSlider.Value = frame.hue;
                saturationSlider.Value = frame.saturation;
                brightnessSlider.Value = frame.brightness;
                delayInput.Text = frame.transitionPeriod.ToString();
            });

            if (brightnessSlider.Value == 0) await tpLink.SetLightState(_activeDevice, false, frame.transitionPeriod);
            else
            {
                await tpLink.SetLightState(_activeDevice, true, frame.transitionPeriod, new TPLink.SPowerOptions
                {
                    hue = frame.hue,
                    saturation = frame.saturation,
                    brightness = frame.brightness
                });
            }

            UpdateSliderThemes(false);
        }

        private void deleteFrame_click(object sender, RoutedEventArgs e)
        {
            if (activeScript.Item1 == null || activeScript.Item2 == -1) return;
            for (int i = activeScript.Item2; i < scriptFrames.Items.Count; i++)
            {
                ListBoxItem item = (ListBoxItem)scriptFrames.Items.GetItemAt(i);
                item.Tag = (int)item.Tag - 1;
            }
            activeScript.Item3.RemoveAt(activeScript.Item2);
            scriptFrames.Items.RemoveAt(activeScript.Item2);
        }

        private void scriptFrames_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (activeScript.Item1 == null) return;
            activeScript.Item2 = -1;
            delayInput.Text = "100";
        }
    }
}
