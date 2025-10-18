using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.IO;

namespace OllamaChat
{
    public partial class Form1 : Form
    {
        private static readonly HttpClient client = new HttpClient();

        //Serializable replacement for tuple
        public class ChatEntry
        {
            public string Role { get; set; } = "";
            public string Text { get; set; } = "";
        }

        //Conversation history
        private readonly List<ChatEntry> chatHistory = new();

        //File to persist chat memory
        private readonly string historyFile = Path.Combine(Application.StartupPath, "chatHistory.json");

        public Form1()
        {
            InitializeComponent();
            btnSend.Click += async (s, e) => await SendMessage();

            //Enter key sends message, Shift+Enter adds newline
            txtPrompt.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift)
                {
                    e.SuppressKeyPress = true; // prevent newline
                    btnSend.PerformClick();    // trigger send
                }
            };

            //Optional clear button hookup if you add one in designer
            if (btnClearChat != null)
                btnClearChat.Click += (s, e) => ClearChat();

            LoadChatHistory();
        }

        private void AppendText(string text)
        {
            txtChat.AppendText(text);
            txtChat.ScrollToCaret();
        }

        private void AppendColoredText(string prefix, string message, Color color)
        {
            int start = txtChat.TextLength;
            txtChat.SelectionStart = start;
            txtChat.SelectionColor = color;
            txtChat.AppendText(prefix);
            txtChat.SelectionColor = Color.Black; // reset color for message text
            txtChat.AppendText(message + Environment.NewLine + Environment.NewLine);
            txtChat.ScrollToCaret();
        }


        //---------- PERSISTENCE LOGIC ----------
        private void LoadChatHistory()
        {
            try
            {
                if (File.Exists(historyFile))
                {
                    string json = File.ReadAllText(historyFile);
                    var restored = JsonSerializer.Deserialize<List<ChatEntry>>(json);

                    if (restored != null)
                    {
                        chatHistory.Clear();
                        chatHistory.AddRange(restored);

                        foreach (var entry in chatHistory)
                        {
                            //if (entry.Role == "user")
                            //    AppendText($"You: {entry.Text}\n\n");
                            //else if (entry.Role == "assistant")
                            //    AppendText($"Bot: {entry.Text}\n\n");

                            if (entry.Role == "user")
                                AppendColoredText("You: ", entry.Text, Color.Blue);
                            else if (entry.Role == "assistant")
                                AppendColoredText("Bot: ", entry.Text, Color.Red);

                        }
                    }
                }
            }
            catch
            {
                //Ignore corrupt file or deserialization errors
            }
        }

        private void SaveChatHistory()
        {
            try
            {
                string json = JsonSerializer.Serialize(chatHistory, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(historyFile, json);
            }
            catch
            {
                //Ignore write errors
            }
        }

        private void ClearChat()
        {
            //Confirm before clearing
            var result = MessageBox.Show(
                "Are you sure you want to delete all chat history?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result != DialogResult.Yes)
                return;

            chatHistory.Clear();
            txtChat.Clear();

            try
            {
                if (File.Exists(historyFile))
                    File.Delete(historyFile);
            }
            catch
            {
                //Ignore file deletion errors
            }

            AppendText("[Chat history cleared]\n\n");
        }

        //----------------------------------------

        private string BuildPrompt(string newUserMessage)
        {
            var sb = new StringBuilder();

            foreach (var entry in chatHistory)
                sb.AppendLine($"{entry.Role}: {entry.Text}");

            sb.AppendLine($"user: {newUserMessage}");
            sb.AppendLine("assistant:");
            return sb.ToString();
        }

        private async Task SendMessage()
        {
            string prompt = txtPrompt.Text.Trim();
            if (string.IsNullOrEmpty(prompt)) return;

            AppendColoredText("You: ", prompt, Color.Blue);

            txtPrompt.Clear();

            chatHistory.Add(new ChatEntry { Role = "user", Text = prompt });

            string fullPrompt = BuildPrompt(prompt);

            var payload = new
            {
                model = "mistral-en",
                prompt = fullPrompt,
                stream = chkStream.Checked
            };

            string json = JsonSerializer.Serialize(payload);

            try
            {
                string reply = chkStream.Checked
                    ? await SendStreamedRequest(json)
                    : await SendFullRequest(json);

                chatHistory.Add(new ChatEntry { Role = "assistant", Text = reply });

                //Save updated chat to disk
                SaveChatHistory();
            }
            catch (Exception ex)
            {
                AppendText($"[Error: {ex.Message}]\n\n");
            }
        }

        private async Task<string> SendFullRequest(string json)
        {
            var res = await client.PostAsync(
                "http://localhost:11434/api/generate",
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            string responseJson = await res.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(responseJson);
            string? reply = doc.RootElement.GetProperty("response").GetString() ?? "[No response]";

            AppendColoredText("Bot: ", reply, Color.Red);

            return reply;
        }

        private async Task<string> SendStreamedRequest(string json)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:11434/api/generate")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            using var res = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            using var reader = new StreamReader(await res.Content.ReadAsStreamAsync());

            //Write the prefix in red, but without trailing newlines
            int start = txtChat.TextLength;
            txtChat.SelectionStart = start;
            txtChat.SelectionColor = Color.Red;
            txtChat.AppendText("Bot: ");
            txtChat.SelectionColor = Color.Black;

            var sb = new StringBuilder();

            while (!reader.EndOfStream)
            {
                string? line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    using var doc = JsonDocument.Parse(line);
                    if (doc.RootElement.TryGetProperty("response", out var token))
                    {
                        string? fragment = token.GetString();
                        if (!string.IsNullOrEmpty(fragment))
                        {
                            sb.Append(fragment);
                            txtChat.AppendText(fragment);
                            txtChat.ScrollToCaret();
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }

            string reply = sb.ToString().Trim();
            txtChat.AppendText("\n\n");
            txtChat.ScrollToCaret();
            return reply;
        }

    }
}