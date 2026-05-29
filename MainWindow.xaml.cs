using System;
using System.Media;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ChatBotP2
{
    public partial class MainWindow : Window
    {
        private readonly ChatBot _bot = new ChatBot();
        private bool _moodSelected = false;
        private bool _nameCollected = false;
        private readonly DispatcherTimer _clock = new DispatcherTimer();

        private static readonly Color PurpleColor = Color.FromRgb(110, 64, 201);

        public MainWindow()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        // ══════════════════════════════════════════════
        //  STARTUP
        // ══════════════════════════════════════════════
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            _clock.Interval = TimeSpan.FromSeconds(1);
            _clock.Tick += (s, a) => ClockLabel.Text = DateTime.Now.ToString("HH:mm:ss");
            _clock.Start();
            ClockLabel.Text = DateTime.Now.ToString("HH:mm:ss");

            PlayVoice();
            await Task.Delay(300);

            AddBotBubble("Hello! I am here to help you stay safe online.");
            await Task.Delay(500);
            AddBotBubbleWithMoodButtons("How are you feeling today?");

            SetStatus("Select your mood to continue...");
            UserInput.IsEnabled = false;
        }

        // ══════════════════════════════════════════════
        //  BOT BUBBLE — plain message
        // ══════════════════════════════════════════════
        private void AddBotBubble(string message)
        {
            var row = MakeRow(isUser: false);
            row.Children.Add(MakeBotAvatar());

            var col = new StackPanel();
            col.Children.Add(MakeSenderLabel("Bot", isUser: false));
            col.Children.Add(MakeBubble(message, isUser: false));
            col.Children.Add(MakeTimeStamp(isUser: false));
            row.Children.Add(col);

            ChatPanel.Children.Add(row);
            ChatScroll.ScrollToBottom();
        }

        // ══════════════════════════════════════════════
        //  BOT BUBBLE — with 3 mood buttons inside
        // ══════════════════════════════════════════════
        private void AddBotBubbleWithMoodButtons(string question)
        {
            var row = MakeRow(isUser: false);
            row.Children.Add(MakeBotAvatar());

            var col = new StackPanel();
            col.Children.Add(MakeSenderLabel("Bot", isUser: false));

          var bubble = new Border
{
    Background = new SolidColorBrush(Colors.Black),
    CornerRadius = new CornerRadius(0, 10, 10, 10),
    Padding = new Thickness(14, 11, 14, 11),
    MaxWidth = 420
};

            var inner = new StackPanel();
            inner.Children.Add(new TextBlock
            {
                Text = question,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.FromRgb(201, 209, 217)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            });

            var btnRow = new StackPanel { Orientation = Orientation.Horizontal };

            var happy = MakeMoodButton("Happy ");
            var neutral = MakeMoodButton("Neutral ");
            var sad = MakeMoodButton("Sad ");

            happy.Click += (s, e) => OnMoodSelected("happy", "😊");
            neutral.Click += (s, e) => OnMoodSelected("neutral", "😐");
            sad.Click += (s, e) => OnMoodSelected("sad", "😢");

            btnRow.Children.Add(happy);
            btnRow.Children.Add(neutral);
            btnRow.Children.Add(sad);

            inner.Children.Add(btnRow);
            bubble.Child = inner;

            col.Children.Add(bubble);
            col.Children.Add(MakeTimeStamp(isUser: false));
            row.Children.Add(col);

            ChatPanel.Children.Add(row);
            ChatScroll.ScrollToBottom();
        }

        // ══════════════════════════════════════════════
        //  USER BUBBLE
        // ══════════════════════════════════════════════
        private void AddUserBubble(string message)
        {
            var row = MakeRow(isUser: true);

            var col = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
            col.Children.Add(MakeSenderLabel("You", isUser: true));
            col.Children.Add(MakeBubble(message, isUser: true));
            col.Children.Add(MakeTimeStamp(isUser: true));

            row.Children.Add(col);
            row.Children.Add(MakeUserAvatar());

            ChatPanel.Children.Add(row);
            ChatScroll.ScrollToBottom();
        }

        // ══════════════════════════════════════════════
        //  MOOD SELECTION
        // ══════════════════════════════════════════════
        private async void OnMoodSelected(string mood, string emoji)
        {
            if (_moodSelected) return;
            _moodSelected = true;

            AddUserBubble($"{emoji} {CapFirst(mood)}");
            await Task.Delay(400);

            string reply;
            if (mood == "happy")
                reply = "That is wonderful to hear! A positive mindset is a great start.";
            else if (mood == "neutral")
                reply = "That is okay . I am here to help either way.";
            else if (mood == "sad")
                reply = "I am sorry to hear that. I hope I can cheer you up with some helpful tips!";
            else
                reply = "Thanks for letting me know!";

            SentimentLabel.Text = $"{emoji} {mood}";
            AddBotBubble(reply);
            await Task.Delay(400);
            AddBotBubble("What is your name?");
            SetStatus("Please enter your name...");
            UserInput.IsEnabled = true;
            UserInput.Focus();
        }

        // ══════════════════════════════════════════════
        //  INPUT HANDLING
        // ══════════════════════════════════════════════
        private void UserInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) ProcessInput();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e) => ProcessInput();

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ChatPanel.Children.Clear();
            AddBotBubble("Chat cleared. Which topic do you want to browse today?");
        }

        private void TopicButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string topic)
            {
                try
                {
                    var tw = new TopicWindow(_bot, topic) { Owner = this };
                    tw.ShowDialog();
                }
                catch (Exception ex)
                {
                    AddBotBubble($"Could not open topic window: {ex.Message}");
                }
            }
        }

        private async void ProcessInput()
        {
            try
            {
                string raw = UserInput.Text.Trim();
                if (string.IsNullOrWhiteSpace(raw)) return;
                UserInput.Clear();

                // Step 1 — collect name
                if (!_nameCollected)
                {
                    string name = raw.Split(' ')[0];
                    _bot.Memory.Name = CapFirst(name);
                    _nameCollected = true;

                    AddUserBubble(raw);
                    await Task.Delay(400);
                    AddBotBubble($"Hello {_bot.Memory.Name}! What can I help you with today? You can either type or click the buttons below.");
                    SetStatus($"● Logged in as {_bot.Memory.Name}");
                    return;
                }

                AddUserBubble(raw);

                // Sentiment detection — update mood badge
                Sentiment s = _bot.DetectSentiment(raw.ToLower());
                UpdateMoodBadge(s);

                // Topic keyword → open TopicWindow
                string lower = raw.ToLower();
                foreach (string key in new[] { "password", "phishing", "safe browsing", "browsing", "privacy", "scam", "malware" })
                {
                    if (lower.Contains(key))
                    {
                        string topicKey = key == "browsing" ? "safe browsing" : key;
                        var tw = new TopicWindow(_bot, topicKey) { Owner = this };
                        tw.ShowDialog();
                        return;
                    }
                }

                string response = _bot.Respond(raw);
                await Task.Delay(300);
                AddBotBubble(response);

                if (lower == "exit" || lower == "bye" || lower == "quit")
                {
                    await Task.Delay(1200);
                    Application.Current.Shutdown();
                }

                SetStatus(string.IsNullOrEmpty(_bot.Memory.LastTopic)
                    ? "● Ready"
                    : $"● Last topic: {_bot.Memory.LastTopic} type 'more' for another tip");
            }
            catch (Exception ex)
            {
                AddBotBubble($"Something went wrong: {ex.Message}. Please try again.");
            }
        }

        // ══════════════════════════════════════════════
        //  UI ELEMENT BUILDERS
        // ══════════════════════════════════════════════

        private static StackPanel MakeRow(bool isUser) => new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, 12),
            MaxWidth = 620
        };

        // Purple robot avatar for bot
        private static Border MakeBotAvatar()
        {
            var canvas = new Canvas { Width = 24, Height = 24 };

            var head = new System.Windows.Shapes.Rectangle
            { Width = 18, Height = 13, RadiusX = 3, RadiusY = 3, Fill = Brushes.White };
            canvas.Children.Add(head);
            Canvas.SetLeft(head, 3); Canvas.SetTop(head, 7);

            var eyeL = new System.Windows.Shapes.Ellipse
            { Width = 3, Height = 3, Fill = new SolidColorBrush(PurpleColor) };
            canvas.Children.Add(eyeL);
            Canvas.SetLeft(eyeL, 7); Canvas.SetTop(eyeL, 11);

            var eyeR = new System.Windows.Shapes.Ellipse
            { Width = 3, Height = 3, Fill = new SolidColorBrush(PurpleColor) };
            canvas.Children.Add(eyeR);
            Canvas.SetLeft(eyeR, 14); Canvas.SetTop(eyeR, 11);

            var mouth = new System.Windows.Shapes.Rectangle
            { Width = 8, Height = 2, RadiusX = 1, RadiusY = 1, Fill = new SolidColorBrush(PurpleColor) };
            canvas.Children.Add(mouth);
            Canvas.SetLeft(mouth, 8); Canvas.SetTop(mouth, 16);

            var stem = new System.Windows.Shapes.Rectangle
            { Width = 2, Height = 4, Fill = Brushes.White };
            canvas.Children.Add(stem);
            Canvas.SetLeft(stem, 11); Canvas.SetTop(stem, 3);

            var tip = new System.Windows.Shapes.Ellipse
            { Width = 4, Height = 4, Fill = Brushes.White };
            canvas.Children.Add(tip);
            Canvas.SetLeft(tip, 10); Canvas.SetTop(tip, 0);

            return new Border
            {
                Width = 36,
                Height = 36,
                CornerRadius = new CornerRadius(18),
                Background = new SolidColorBrush(PurpleColor),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Top,
                Child = new Viewbox
                {
                    Width = 24,
                    Height = 24,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = canvas
                }
            };
        }

        // Purple person avatar for user
        private static Border MakeUserAvatar()
        {
            var canvas = new Canvas { Width = 24, Height = 24 };

            var head = new System.Windows.Shapes.Ellipse
            { Width = 10, Height = 10, Fill = Brushes.White };
            canvas.Children.Add(head);
            Canvas.SetLeft(head, 7); Canvas.SetTop(head, 1);

            var body = new System.Windows.Shapes.Rectangle
            { Width = 16, Height = 9, RadiusX = 5, RadiusY = 5, Fill = Brushes.White };
            canvas.Children.Add(body);
            Canvas.SetLeft(body, 4); Canvas.SetTop(body, 13);

            return new Border
            {
                Width = 36,
                Height = 36,
                CornerRadius = new CornerRadius(18),
                Background = new SolidColorBrush(PurpleColor),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Top,
                Child = new Viewbox
                {
                    Width = 24,
                    Height = 24,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Child = canvas
                }
            };
        }

        private static TextBlock MakeSenderLabel(string name, bool isUser) => new TextBlock
        {
            Text = name,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)),
            Margin = new Thickness(0, 0, 0, 3),
            HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left
        };

        private static Border MakeBubble(string message, bool isUser)
        {
            Color bg = isUser ? Color.FromRgb(55, 30, 100) : Color.FromRgb(22, 27, 34);
            var corner = isUser ? new CornerRadius(10, 0, 10, 10) : new CornerRadius(0, 10, 10, 10);
            return new Border
            {
                Background = new SolidColorBrush(bg),
                CornerRadius = corner,
                Padding = new Thickness(13, 10, 13, 10),
                MaxWidth = 460,
                Child = new TextBlock
                {
                    Text = message,
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(201, 209, 217)),
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 20
                }
            };
        }

        private static TextBlock MakeTimeStamp(bool isUser) => new TextBlock
        {
            Text = DateTime.Now.ToString("HH:mm"),
            FontFamily = new FontFamily("Consolas"),
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.FromRgb(72, 79, 88)),
            Margin = new Thickness(0, 3, 0, 0),
            HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left
        };

        // Purple mood button with white bold text
        private static Button MakeMoodButton(string label)
        {
            var btn = new Button
            {
                Content = label,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(PurpleColor),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 8, 0)
            };

            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetBinding(Border.BackgroundProperty,
                new System.Windows.Data.Binding("Background")
                {
                    RelativeSource = new System.Windows.Data.RelativeSource(
                        System.Windows.Data.RelativeSourceMode.TemplatedParent)
                });
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(20));
            border.SetValue(Border.PaddingProperty, new Thickness(14, 7, 14, 7));
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(cp);
            template.VisualTree = border;
            btn.Template = template;

            var hoverColor = Color.FromRgb(138, 92, 246);
            btn.MouseEnter += (s, e) => btn.Background = new SolidColorBrush(hoverColor);
            btn.MouseLeave += (s, e) => btn.Background = new SolidColorBrush(PurpleColor);
            return btn;
        }

        // ══════════════════════════════════════════════
        //  STATUS + MOOD BADGE
        // ══════════════════════════════════════════════
        private void SetStatus(string msg) => StatusBar.Text = msg;

        private void UpdateMoodBadge(Sentiment s)
        {
            string text;
            Color colour;

            if (s == Sentiment.Worried)
            {
                text = "worried";
                colour = Color.FromRgb(255, 160, 40);
            }
            else if (s == Sentiment.Curious)
            {
                text = "curious";
                colour = Color.FromRgb(0, 200, 255);
            }
            else if (s == Sentiment.Frustrated)
            {
                text = "frustrated";
                colour = Color.FromRgb(255, 80, 80);
            }
            else if (s == Sentiment.Happy)
            {
                text = "happy";
                colour = Color.FromRgb(100, 230, 100);
            }
            else
            {
                text = "neutral";
                colour = Color.FromRgb(196, 181, 253);
            }

            SentimentLabel.Text = text;
            SentimentLabel.Foreground = new SolidColorBrush(colour);
        }

        // ══════════════════════════════════════════════
        //  VOICE + UTILITY
        // ══════════════════════════════════════════════
        private void PlayVoice()
        {
            try
            {

                string exePath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "voice_audio.wav");
                string srcPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..", "voice_audio.wav");
                string srcPathFull = System.IO.Path.GetFullPath(srcPath);

                string finalPath = System.IO.File.Exists(exePath) ? exePath :
                                   System.IO.File.Exists(srcPathFull) ? srcPathFull : null;

                if (finalPath != null)
                {
                    SoundPlayer player = new SoundPlayer(finalPath);
                    player.LoadAsync();           // non-blocking load
                    player.PlaySync();            // plays once fully
                }
                else
                {
                    // File not found — silently skip, bot still works
                    System.Diagnostics.Debug.WriteLine(
                        "voice_audio.wav not found.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Voice error: " + ex.Message);
            }
        }

        private static string CapFirst(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1).ToLower();
    }
}

