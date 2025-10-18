using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OllamaChat
{
    public partial class Form1 : Form
    {
        private string modelToUse = "mistral-en";//"phi3-custom"; //"mistral-en";
        private static readonly HttpClient client = new HttpClient();

        // Serializable replacement for tuple
        public class ChatEntry
        {
            public string Role { get; set; } = "";
            public string Text { get; set; } = "";
        }

        // Conversation history
        private readonly List<ChatEntry> chatHistory = new();

        // File to persist chat memory (set dynamically)
        private string historyFile;

        // --- Improved code block rendering ---
        private void DisplayBotReply(string reply)
        {
            // Split on ``` while keeping order of text/code sections
            var parts = Regex.Split(reply, "```", RegexOptions.Multiline);
            bool insideCode = false;

            foreach (var raw in parts)
            {
                string segment = raw.Trim('\r', '\n');

                if (insideCode)
                {
                    // Remove language identifiers (csharp, cs, etc.)
                    if (segment.StartsWith("csharp", StringComparison.OrdinalIgnoreCase) ||
                        segment.StartsWith("cs", StringComparison.OrdinalIgnoreCase))
                    {
                        int newlineIndex = segment.IndexOf('\n');
                        if (newlineIndex > 0)
                            segment = segment.Substring(newlineIndex + 1);
                    }

                    // Detect language from fence tag if available
                    string language = "C#";
                    if (raw.StartsWith("python", StringComparison.OrdinalIgnoreCase))
                        language = "Python";
                    else if (raw.StartsWith("sql", StringComparison.OrdinalIgnoreCase))
                        language = "SQL";

                    AppendCodeBlock(segment.Trim(), language);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(segment))
                        AppendColoredText("Bot: ", segment, Color.Red);
                }

                insideCode = !insideCode;
            }
        }

        private string GetSafeHistoryFileName(string modelName)
        {
            string safeName = string.Join("_", modelName.Split(Path.GetInvalidFileNameChars()))
                .Replace(":", "_")
                .Replace("/", "_")
                .Trim();

            return Path.Combine(Application.StartupPath, $"chatHistory_{safeName}.json");
        }

        private async Task<List<string>> GetInstalledModelsAsync()
        {
            try
            {
                var response = await client.GetAsync("http://localhost:11434/api/tags");
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var models = new List<string>();

                foreach (var model in doc.RootElement.GetProperty("models").EnumerateArray())
                {
                    string name = model.GetProperty("name").GetString() ?? "";
                    if (!string.IsNullOrEmpty(name))
                    {
                        // remove “:latest” if present
                        name = name.Replace(":latest", "").Trim();
                        models.Add(name);
                    }
                }

                return models;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fetching models: {ex.Message}", "Ollama", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new List<string>();
            }
        }

        private async Task LoadModelsIntoDropdown()
        {
            var models = await GetInstalledModelsAsync();
            cmbModels.Items.Clear();
            cmbModels.Items.AddRange(models.ToArray());

            if (models.Count > 0)
            {
                cmbModels.SelectedIndex = 0;
                modelToUse = models[0];
                //historyFile = Path.Combine(Application.StartupPath, $"chatHistory_{modelToUse}.json");

                string safeModelName = string.Join("_", modelToUse.Split(Path.GetInvalidFileNameChars()))
                                .Replace(":", "_")
                                .Replace("/", "_");

                historyFile = Path.Combine(Application.StartupPath, $"chatHistory_{safeModelName}.json");

                LoadChatHistory();
            }

            cmbModels.SelectedIndexChanged += (s, e) =>
            {
                modelToUse = cmbModels.SelectedItem?.ToString() ?? modelToUse;
                //historyFile = Path.Combine(Application.StartupPath, $"chatHistory_{modelToUse}.json");

                string safeModelName = string.Join("_", modelToUse.Split(Path.GetInvalidFileNameChars()))
                                .Replace(":", "_")
                                .Replace("/", "_");

                historyFile = Path.Combine(Application.StartupPath, $"chatHistory_{safeModelName}.json");

                txtChat.Clear();
                chatHistory.Clear();
                LoadChatHistory();
            };
        }

        private void AppendCodeBlock(string code, string language = "C#")
        {
            txtChat.AppendText(Environment.NewLine);

            // --- Header bar ---
            int headerStart = txtChat.TextLength;
            txtChat.SelectionStart = headerStart;
            txtChat.SelectionFont = new Font("Segoe UI", 9, FontStyle.Bold);
            txtChat.SelectionColor = Color.White;
            txtChat.SelectionBackColor = Color.FromArgb(60, 120, 180); // blue-gray bar
            txtChat.AppendText($"   {language} code   ");
            txtChat.AppendText(Environment.NewLine);

            // --- Code body ---
            int start = txtChat.TextLength;
            txtChat.SelectionStart = start;
            txtChat.SelectionColor = Color.DarkGreen;
            txtChat.SelectionBackColor = Color.FromArgb(240, 240, 240);
            txtChat.SelectionFont = new Font("Consolas", 10, FontStyle.Regular);

            txtChat.AppendText(code.Trim() + Environment.NewLine + Environment.NewLine);

            // --- Reset formatting ---
            txtChat.SelectionBackColor = txtChat.BackColor;
            txtChat.SelectionColor = Color.Black;
            txtChat.SelectionFont = new Font("Segoe UI", 10, FontStyle.Regular);

            txtChat.SelectionStart = txtChat.TextLength;
            txtChat.ScrollToCaret();
        }

        public Form1()
        {
            InitializeComponent();

            txtChat.Font = new Font("Consolas", 10, FontStyle.Regular);
            txtChat.WordWrap = false;

            cmbModels.DropDownStyle = ComboBoxStyle.DropDownList;

            // Load models dynamically
            _ = LoadModelsIntoDropdown();

            btnSend.Click += async (s, e) => await SendMessage();

            // Enter key sends message, Shift+Enter adds newline
            txtPrompt.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift)
                {
                    e.SuppressKeyPress = true;
                    btnSend.PerformClick();
                }
            };

            if (btnClearChat != null)
                btnClearChat.Click += (s, e) => ClearChat();
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

        // ---------- PERSISTENCE LOGIC ----------
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
                // Ignore corrupt file or deserialization errors
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
                // Ignore write errors
            }
        }

        private void ClearChat()
        {
            // Confirm before clearing
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
                // Ignore file deletion errors
            }

            AppendText("[Chat history cleared]\n\n");
        }

        // ----------------------------------------

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
                model = modelToUse,
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

                // Save updated chat to disk
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

            //AppendColoredText("Bot: ", reply, Color.Red);

            DisplayBotReply(reply);

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

            // Write the prefix in red, but without trailing newlines
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

            // Re-render formatted version if it contains code fences
            if (reply.Contains("```"))
            {
                txtChat.Clear();
                DisplayBotReply(reply);
            }

            return reply;


            //txtChat.AppendText("\n\n");
            //txtChat.ScrollToCaret();
            //return reply;
        }
    }
}
