using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChatBotP2
{
    public partial class TopicWindow : Window
    {
        private readonly ChatBot _bot;

        private static readonly Dictionary<string, (string heading, string description, string example, List<string> tips)> TopicData =
            new Dictionary<string, (string, string, string, List<string>)>
            {
                ["password"] = (
                "Password Safety",
                "In order to keep your data protected from unauthorised users, you must use strong passwords with symbols, numbers, and letters.",
                "Example of a strong password: @KI32We$$",
                new List<string>
                {
                    "Avoid using personal information such as your first name or birthdate.",
                    "Use a different password for every account you own.",
       
                    "Use a password manager to keep track of your passwords safely.",
                    "A strong password has at least 12 characters: uppercase, lowercase, numbers and symbols."
                }
            ),
                ["phishing"] = (
                "Phishing",
                "Phishing is when someone pretends to be another person or from a company you trust so that they can trick you into giving up your private information.",
                "A phishing email might say: 'Your account will be closed  so click here to verify.' Do NOT click.",
                new List<string>
                {
                    "Signs of phishing: asks you to verify information via a link.",
                    "Do not click links in unexpected messages.",
                    "Check the sender's actual email address, not just the display name.",
                    "Scammers often create urgency. 'Act now!' is a red flag.",
                    "When in doubt, go directly to the company's official website."
                }
            ),
                ["safe browsing"] = (
                "Safe Browsing",
                "Safe browsing is the set of practices you use to reduce risk while you are on the internet.",
                "The 'S' in HTTPS and the padlock icon in your address bar mean the connection is encrypted.",
                new List<string>
                {
                    "Always look for HTTPS and the padlock before entering personal details.",
                    "Use an ad blocker and pop-up blocker to stop malvertising.",
                    "Keep your browser and extensions up to date.",
                    "Avoid public Wi-Fi for banking  use a VPN if you must.",
                    "Clear your cookies and cache regularly to reduce tracking."
                }
            ),
                ["privacy"] = (
                "Privacy",
                "Protecting your privacy online means controlling who can see your personal information and how it is used.",
                "Check your social media privacy settings — are your posts visible to everyone?",
                new List<string>
                {
                    "Review app permissions regularly — does that app really need your location?",
                    "Use a privacy-focused search engine like DuckDuckGo.",
                    "Check your social media privacy settings every few months.",
                    "Be mindful of what personal info you share publicly online.",
                    "Read privacy policies before signing up for new services."
                }
            ),
                ["scam"] = (
                "Scams",
                "Scams are fraudulent schemes designed to trick you into giving money or personal information to criminals.",
                "If someone asks you to pay with gift cards, it is almost certainly a scam.",
                new List<string>
                {
                    "If an offer sounds too good to be true, it almost certainly is.",
                    "Never send money or gift cards to someone you have not met in person.",
                    "Verify requests by calling the organisation using their official number.",
                    "Scammers may impersonate banks, government agencies, or family members.",
                    "Report scams to your country's consumer protection authority."
                }
            ),
                ["malware"] = (
                "Malware",
                "Malware is malicious software designed to damage, disrupt, or gain unauthorised access to your device or data.",
                "Ransomware encrypts your files and demands payment — regular backups are your best defence.",
                new List<string>
                {
                    "Only download software from official or trusted sources.",
                    "Keep your operating system and antivirus software updated.",
                    "Do not plug in unknown USB drives.",
                    "Run a full antivirus scan if your device behaves strangely.",
                    "Back up your data regularly to an offline or cloud location."
                }
            )
            };

        
        private List<string> _currentTips = new List<string>();
        private int _currentTipIndex = 0;

        public TopicWindow(ChatBot bot, string topicKey)
        {
            InitializeComponent();
            _bot = bot;
            LoadTopic(topicKey.ToLower());
        }

        // Looks up the topic in the static Dictionary using the keyword as the key.
        // Uses ItemsSource data binding to populate the tips list automatically -
        // WPF generates one visual item per entry in the List<string>.
        // _currentTipIndex tracks which tip the user is on for the Next Tip feature.
        private void LoadTopic(string key)
        {
            if (!TopicData.TryGetValue(key, out var data)) return;

            Title = $"CyberBot — {data.heading}";
            TopicTitle.Text = data.heading;
            TopicHeading.Text = data.heading;
            TopicDescription.Text = data.description;
            TopicExample.Text = data.example;
            TipsList.ItemsSource = data.tips;

           
            _currentTips = new List<string>(data.tips);
            _currentTipIndex = 0;
            StatusBar.Text = $"● Viewing: {data.heading}  |  Tip 1 of {data.tips.Count}  |  Type a question below";
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) => Close();

        private void FollowUpInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) HandleFollowUp();
        }

        private void AskButton_Click(object sender, RoutedEventArgs e)
        {
           
            if (sender is Button btn && btn.Tag is string t && t == "more")
            {
                FollowUpInput.Text = "more";
            }
            HandleFollowUp();
        }

        // Handles follow-up questions on the topic page.
        // If the user types "more" or "next", modulo arithmetic wraps
        // the index back to zero when the end of the tips List is reached.
        // Otherwise passes the input to ChatBot.Respond() for a dynamic reply.
        private void HandleFollowUp()
        {
            string input = FollowUpInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(input)) return;

           
            string lower = input.ToLower();
            if (lower == "more" || lower == "next" || lower == "another")
            {
                _currentTipIndex = (_currentTipIndex + 1) % _currentTips.Count;
                FollowUpResponse.Text = _currentTips[_currentTipIndex];
                FollowUpBorder.Visibility = Visibility.Visible;
                StatusBar.Text = $"Showing tip {_currentTipIndex + 1} of {_currentTips.Count}";
                FollowUpInput.Clear();
                return;
            }
            

            string response = _bot.Respond(input);
            FollowUpResponse.Text = response;
            FollowUpBorder.Visibility = Visibility.Visible;
            FollowUpInput.Clear();
        }
    }
}
