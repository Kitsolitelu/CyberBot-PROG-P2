using System;
using System.Collections.Generic;
using System.Linq;

namespace ChatBotP2
{
    public delegate string TopicHandler(string input);

    public enum Sentiment { Neutral, Worried, Curious, Frustrated, Happy }

    public class UserMemory
    {
        public string Name { get; set; } = "";
        public string FavouriteTopic { get; set; } = "";
        public string LastTopic { get; set; } = "";
    }

    public class ChatBot
    {
        public UserMemory Memory { get; } = new UserMemory();

        private readonly Random _rng = new Random();

        private readonly Dictionary<string, List<string>> _responses;
        private readonly Dictionary<string, TopicHandler> _handlers;
        private readonly Dictionary<Sentiment, List<string>> _sentimentWords;
        private readonly List<string> _moreWords = new List<string>
            { "more", "another", "tell me more", "another tip", "keep going", "continue", "explain further" };

        public ChatBot()
        {
            _responses = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["password"] = new List<string>
                {
                    "Use a passphrase — four random words together are stronger than a short complex password.",
                    "Never reuse passwords across different sites.",
                    "Enable two-factor authentication  wherever possible for an extra layer of security.",
                    "Avoid using personal info like your name, birthday, or pet's name in passwords.",
                    "A strong password has at least 12 characters: uppercase, lowercase, numbers and symbols. Example: @KI32We$$"
                },
                ["phishing"] = new List<string>
                {
                    "Be cautious of emails asking for personal information as legitimate companies rarely do this.",
                    "Check the sender's actual email address, not just the display name.",
                    "Scammers often create urgency 'Act now!' is a major red flag.",
                    "When in doubt, go directly to the company's official website rather than clicking a link."
                },
                ["safe browsing"] = new List<string>
                {
                    "Always look for HTTPS and the padlock icon before entering any personal details.",
                    "Keep your browser and extensions up to date to patch security vulnerabilities.",
                    "Avoid public Wi-Fi for banking or sensitive tasks . Please use a VPN if you must.",
                    "Clear your cookies and cache regularly to reduce tracking."
                },
                ["privacy"] = new List<string>
                {
                    "Review app permissions regularly  and check if it does that flashlight app really need your contacts?",
                    "Use a privacy-focused search engine like DuckDuckGo to reduce data collection.",
                    "Check your social media privacy settings at least every six months.",
                    "Be mindful of what personal information you share publicly online.",
                    "Read privacy policies before signing up for new services."
                },
                ["scam"] = new List<string>
                {
                    "If an offer sounds too good to be true, it almost certainly is.",
                    "Never send money or gift cards to someone you have not met in person.",
                    "Verify requests by calling the organisation directly using their official number.",
                    "Scammers may impersonate banks, government agencies, or even family members.",
                    "Report scams to your country's consumer protection authority."
                },
                ["malware"] = new List<string>
                {
                    "Only download software from official or well-known trusted sources.",
                    "Keep your operating system and antivirus software updated at all times.",
                    "Do not plug in unknown USB drives — they can auto-run malicious code.",
                    "Ransomware can encrypt your files; regular offline backups are your best defence.",
                    "If your device behaves strangely, run a full antivirus scan immediately."
                }
            };

            _sentimentWords = new Dictionary<Sentiment, List<string>>
            {
                [Sentiment.Worried] = new List<string> { "worried", "scared", "afraid", "anxious", "unsafe", "vulnerable", "fear", "nervous" },
                [Sentiment.Curious] = new List<string> { "curious", "wondering", "interested", "tell me", "how does", "what is", "explain", "learn" },
                [Sentiment.Frustrated] = new List<string> { "frustrated", "annoyed", "confused", "complicated", "difficult", "lost", "hard", "don't understand" },
                [Sentiment.Happy] = new List<string> { "great", "thanks", "awesome", "helpful", "love", "perfect", "cool", "good" }
            };

            _handlers = new Dictionary<string, TopicHandler>(StringComparer.OrdinalIgnoreCase)
            {
                ["password"] = input => GetRandom("password"),
                ["phishing"] = input => GetRandom("phishing"),
                ["safe browsing"] = input => GetRandom("safe browsing"),
                ["browsing"] = input => GetRandom("safe browsing"),
                ["privacy"] = input => GetRandom("privacy"),
                ["scam"] = input => GetRandom("scam"),
                ["malware"] = input => GetRandom("malware"),
                ["virus"] = input => GetRandom("malware"),
                ["2fa"] = input => "Two-Factor Authentication  adds a second step , usually a code sent to your phone — making it much harder for attackers to access your account even with your password.",
                ["vpn"] = input => "A VPN encrypts your internet traffic, hiding your activity from your ISP and others on the same network. Especially useful on public Wi-Fi.",
                ["ransomware"] = input => "Ransomware encrypts your files and demands payment. Prevention: keep backups offline, never open unexpected email attachments, keep software updated.",
                ["antivirus"] = input => "Keep your antivirus updated and run full scans weekly. Windows Defender is decent; Malwarebytes is a strong free complement.",
            };
        }

        // This is the main method called by the GUI every time the user sends a message.
        // It checks conditions in order: name, favourite topic, follow-up phrases,
        // farewells, keyword dictionary match, and finally a default fallback for error handling.
        // Using a Dictionary here instead of if-else chains improves performance
        // and makes it easy to add new topics without changing the logic.
        public string Respond(string userInput)
        {
            if (string.IsNullOrWhiteSpace(userInput))
                return "Could you please type something else? Got lost ";

            string input = userInput.Trim().ToLower();
            Sentiment sentiment = DetectSentiment(input);

            
            if (input.StartsWith("my name is "))
            {
                string name = Capitalise(input.Substring(11).Trim().Split(' ')[0]);
                Memory.Name = name;
                return "Nice to meet you, " + name + "! You can ask me anything regarding password safety, phishing, malware, privacy, scams, safe browsing and more.";
            }

         
            if (input.Contains("i'm interested in") || input.Contains("i am interested in") || input.Contains("my favourite topic is"))
            {
                string topic = ExtractInterest(input);
                if (!string.IsNullOrEmpty(topic))
                {
                    Memory.FavouriteTopic = topic;
                    return "Great! I will remember that you are interested in " + topic + ". It is a crucial part of staying safe online. Would you like a tip?";
                }
            }

            
            if (_moreWords.Any(k => input.Contains(k)))
            {
                if (!string.IsNullOrEmpty(Memory.LastTopic))
                    return GetRandom(Memory.LastTopic);
                return "Sure! Which topic would you like more on : password, phishing, privacy, scams, malware, or safe browsing?";
            }

            
            if (input == "exit" || input == "bye" || input == "quit" || input.Contains("goodbye"))
            {
                string n = string.IsNullOrEmpty(Memory.Name) ? "there" : Memory.Name;
                return "Goodbye, " + n + "! And remember to stay safe online";
            }

            
            if (input == "help" || input == "menu" || input == "topics")
                return "I can help with:\n• Password Safety\n• Phishing\n• Safe Browsing\n• Privacy\n• Scams\n• Malware\n• 2FA, VPN, Ransomware, Antivirus\n\nType a keyword or click a button!";

            
            if (!string.IsNullOrEmpty(Memory.FavouriteTopic) &&
               (input.Contains("remember") || input.Contains("what do you know about me") || input.Contains("my topic")))
            {
                string n = string.IsNullOrEmpty(Memory.Name) ? "You" : Memory.Name;
                return n + ", you mentioned you are interested in " + Memory.FavouriteTopic +
                       ". As someone interested in " + Memory.FavouriteTopic +
                       ", here is a tip just for you: " + GetRandom(Memory.FavouriteTopic);
            }

            
            if (!string.IsNullOrEmpty(Memory.FavouriteTopic) && input.Contains(Memory.FavouriteTopic.ToLower()))
            {
                Memory.LastTopic = Memory.FavouriteTopic;
                string response2 = GetRandom(Memory.FavouriteTopic);
                string n2 = string.IsNullOrEmpty(Memory.Name) ? "" : Memory.Name + ", ";
                return n2 + "as someone interested in " + Memory.FavouriteTopic + ", you might want to know: " + response2;
            }

            
            foreach (var entry in _handlers)
            {
                if (input.Contains(entry.Key))
                {
                    Memory.LastTopic = entry.Key;
                    string response = entry.Value(input);
                    return WrapSentiment(response, sentiment);
                }
            }

            
            string name2 = string.IsNullOrEmpty(Memory.Name) ? "" : ", " + Memory.Name;
            return "I am not sure I understand that" + name2 + ". Could you try rephrasing?\nTry typing 'help' to see all available topics.";
        }

        public Sentiment DetectSentiment(string input)
        {
            foreach (var entry in _sentimentWords)
                if (entry.Value.Any(k => input.Contains(k)))
                    return entry.Key;
            return Sentiment.Neutral;
        }

        private string WrapSentiment(string response, Sentiment s)
        {
            switch (s)
            {
                case Sentiment.Worried: return "It is completely understandable to feel that way. Here is something that can help:\n\n" + response;
                case Sentiment.Curious: return "Great question! Here is what you should know:\n\n" + response;
                case Sentiment.Frustrated: return "No worries,let me break it down simply:\n\n" + response;
                case Sentiment.Happy: return "Glad you are engaged! Here is a tip:\n\n" + response;
                default: return response;
            }
        }

        private string GetRandom(string topic)
        {
            if (_responses.TryGetValue(topic, out var pool) && pool.Count > 0)
                return pool[_rng.Next(pool.Count)];
            return "That is an important  topic. Could you be more specific?";
        }

        private string ExtractInterest(string input)
        {
            string[] prefixes = { "i'm interested in ", "i am interested in ", "my favourite topic is " };
            foreach (string p in prefixes)
            {
                int idx = input.IndexOf(p, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                    return input.Substring(idx + p.Length).Trim().TrimEnd('.', '!', '?');
            }
            return "";
        }

        private static string Capitalise(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}

