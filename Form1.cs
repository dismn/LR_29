using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;


namespace LR_29
{
    public partial class Form1 : Form
    {
        bool alive = false; // чи буде працювати потік для приймання
        UdpClient client;
        const int LOCALPORT = 8001; // порт для приймання повідомлень
        const int REMOTEPORT = 8001; // порт для передавання повідомлень
        const int TTL = 20;
        const string HOST = "235.5.5.1"; // хост для групового розсилання
        IPAddress groupAddress; // адреса для групового розсилання
        string userName; // ім’я користувача в чаті
        Font selectedFont;
        Color selectedColor;

        public Form1()
        {
            InitializeComponent();
            loginButton.Enabled = true; // кнопка входу
            logoutButton.Enabled = false; // кнопка виходу
            sendButton.Enabled = false; // кнопка отправки
            chatTextBox.ReadOnly = true; // поле для повідомлень
            groupAddress = IPAddress.Parse(HOST);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void loginButton_Click(object sender, EventArgs e)
        {
            userName = userNameTextBox.Text;
            userNameTextBox.ReadOnly = true;
            try
            {
                client = new UdpClient(LOCALPORT);
                //підєднання до групового розсилання
                client.JoinMulticastGroup(groupAddress, TTL);

                // задача на приймання повідомлень
                Task receiveTask = new Task(ReceiveMessages);
                receiveTask.Start();
                // перше повідомлення про вхід нового користувача
                string message = userName + " вошел в чат";
                byte[] data = Encoding.Unicode.GetBytes(message);
                client.Send(data, data.Length, HOST, REMOTEPORT);
                loginButton.Enabled = false;
                logoutButton.Enabled = true;
                sendButton.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        // метод приймання повідомлення
        private void ReceiveMessages()
        {
            alive = true;
            try
            {
                while (alive)
                {
                    IPEndPoint remoteIp = null;
                    byte[] data = client.Receive(ref remoteIp);
                    string message = Encoding.Unicode.GetString(data);
                    // добавляем полученное сообщение в текстовое поле
                    this.Invoke(new MethodInvoker(() =>
                    {
                        string time = DateTime.Now.ToShortTimeString();
                        chatTextBox.Text = time + " " + message + "\r\n"
                        + chatTextBox.Text;
                    }));
                }
            }
            catch (ObjectDisposedException)
            {
                if (!alive)
                    return;
                throw;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void sendButton_Click(object sender, EventArgs e)
        {
            try
            {
                string message = String.Format("{0}: {1}", userName,
               messageTextBox.Text);
                byte[] data = Encoding.Unicode.GetBytes(message);
                client.Send(data, data.Length, HOST, REMOTEPORT);
                messageTextBox.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void logoutButton_Click(object sender, EventArgs e)
        {
            ExitChat();
        }

        private void ExitChat()
        {
            string message = userName + " покидает чат";
            byte[] data = Encoding.Unicode.GetBytes(message);
            client.Send(data, data.Length, HOST, REMOTEPORT);
            client.DropMulticastGroup(groupAddress);
            alive = false;
            client.Close();
            loginButton.Enabled = true;
            logoutButton.Enabled = false;
            sendButton.Enabled = false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (alive)
            ExitChat();
        }

        private void settingsButton_Click(object sender, EventArgs e)
        {
            // Діалогове вікно вибору шрифту
            FontDialog fontDialog = new FontDialog();
            DialogResult fontResult = fontDialog.ShowDialog();

            if (fontResult == DialogResult.OK)
            {
                selectedFont = fontDialog.Font;

                // Оновити вигляд текстового поля
                chatTextBox.Font = selectedFont;
            }

            // Діалогове вікно вибору кольору
            ColorDialog colorDialog = new ColorDialog();
            DialogResult colorResult = colorDialog.ShowDialog();

            if (colorResult == DialogResult.OK)
            {
                selectedColor = colorDialog.Color;

                // Оновити вигляд текстового поля
                chatTextBox.ForeColor = selectedColor;
            }
            ShowSettingsDialog();
        }
        private void ShowSettingsDialog()
        {
            // Створити нову форму налаштувань
            Form settingsForm = new Form();
            settingsForm.Text = "Налаштування чату";
            settingsForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            settingsForm.StartPosition = FormStartPosition.CenterScreen;
            settingsForm.ClientSize = new Size(600, 300);

            // Додати елементи управління до форми налаштувань
            Label addressLabel = new Label();
            addressLabel.Text = "Адреса:";
            addressLabel.Location = new Point(10, 20);

            TextBox addressTextBox = new TextBox();
            addressTextBox.Text = HOST;
            addressTextBox.Location = new Point(150, 20);
            addressTextBox.Size = new Size(200, 20);

            Label portLabel = new Label();
            portLabel.Text = "Порт:";
            portLabel.Location = new Point(10, 50);

            TextBox portTextBox = new TextBox();
            portTextBox.Text = REMOTEPORT.ToString();
            portTextBox.Location = new Point(150, 50);
            portTextBox.Size = new Size(200, 20);

            // Кнопка Зберегти
            Button saveButton = new Button();
            saveButton.Text = "Зберегти";
            saveButton.DialogResult = DialogResult.OK;
            saveButton.Location = new Point(120, 130);
            saveButton.Click += (s, e) =>
            {
                // Зберегти нові значення параметрів чату
                string HOST = addressTextBox.Text;
                int REMOTEPORT = int.Parse(portTextBox.Text);
            };

            // Додати елементи до форми налаштувань
            settingsForm.Controls.AddRange(new Control[] { addressLabel, addressTextBox, portLabel, portTextBox, saveButton });

            // Відобразити форму налаштувань
            settingsForm.ShowDialog();




        }
        //кнопка збереження логу
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // Вибір файлу для збереження логу
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Text Files (*.txt)|*.txt";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog.FileName;

                    // Збереження логу чату у текстовий файл
                    using (StreamWriter writer = new StreamWriter(filePath))
                    {
                        writer.Write(chatTextBox.Text);
                    }

                    MessageBox.Show("Log saved successfully!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
    }
}
