using System;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;
using Util.Configuration;
using System.Xml.Serialization;
using System.IO;
using Computation.Util;
using System.Windows.Threading;
using System.Text;

namespace Computation
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private Configuration _configuration = null;
        private MySqlConnection _sqlConnection = null;
        private DispatcherTimer _ticketTimer = new DispatcherTimer();
        private const string Space = " | ";



        public MainWindow()
        {
            InitializeComponent();
            LoadConfiguration();
            SetUpConnection();

            textBox_firstVote.Focus();
        }




        private void CheckValueFirstVote(object sender, TextChangedEventArgs e)
        {
            switch (textBox_firstVote.Text)
            {
                case "000":
                    textBox_firstVoteList.Text = "Enthaltung";
                    break;

                case "999":
                    textBox_firstVoteList.Text = "Ungültig";
                    break;

                default:
                    if (!textBox_firstVote.Text.Equals(string.Empty))
                    {
                        _sqlConnection.Open();
                        var command = _sqlConnection.CreateCommand();
                        command.CommandText = "SELECT list FROM lists WHERE ldfnr = @lfdnr";
                        command.Parameters.AddWithValue("@lfdnr", textBox_firstVote.Text);
                        command.Prepare();
                        var result = command.ExecuteReader();

                        if (result.Read())
                        {
                            textBox_firstVoteList.Text = result[0].ToString();
                        }
                        else
                        {
                            textBox_firstVoteList.Text = string.Empty;
                        }
                        _sqlConnection.Close();
                    }
                    break;
            }
        }

        private void CheckValueSecondVote(object sender, TextChangedEventArgs e)
        {
            switch (textBox_secondVote.Text)
            {
                case "000":
                    textBox_secondVoteName.Text = "Enthaltung";
                    textBox_secondVoteList.Text = "Enthaltung";
                    textBox_SecondVoteFaculty.Text = "Enthaltung";
                    break;

                case "999":
                    textBox_secondVoteName.Text = "Ungültig";
                    textBox_secondVoteList.Text = "Ungültig";
                    textBox_SecondVoteFaculty.Text = "Ungültig";
                    break;

                default:
                    _sqlConnection.Open();
                    var command = _sqlConnection.CreateCommand();
                    command.CommandText = "SELECT Name, List, Faculty FROM Computation.SecondVoteView WHERE Lfdnr = @lfdnr";
                    command.Parameters.AddWithValue("@lfdnr", textBox_secondVote.Text);
                    command.Prepare();
                    var result = command.ExecuteReader();

                    if (result.Read())
                    {
                        textBox_secondVoteName.Text = result[0].ToString();
                        textBox_secondVoteList.Text = result[1].ToString();
                        textBox_SecondVoteFaculty.Text = result[2].ToString();
                    }
                    else
                    {
                        textBox_secondVoteName.Text = string.Empty;
                        textBox_secondVoteList.Text = string.Empty;
                        textBox_SecondVoteFaculty.Text = string.Empty;
                    }
                    _sqlConnection.Close();
                    break;
            }
        }

        private void Clarification(object sender, RoutedEventArgs e)
        {
            CreateTicket();
        }

        private void CloseTicket(object sender, RoutedEventArgs e)
        {
            if (textBox_ticketID.Text != string.Empty)
            {
                _sqlConnection.Open();
                var command = _sqlConnection.CreateCommand();
                command.CommandText = "UPDATE tickets SET open = '0' WHERE id = @id";
                command.Parameters.AddWithValue("@id", textBox_ticketID.Text);
                command.Prepare();

                command.ExecuteNonQuery();

                _sqlConnection.Close();

                // Clean
                textBox_ticketID.Text = string.Empty;
                textBox_ticketGroup.Text = string.Empty;
                textBox_ticketBallotBox.Text = string.Empty;
                textBox_ticketNumber.Text = string.Empty;

                // Update
                updateTicketsLogic();
            }
        }

        private void CreateTicket()
        {
            int numberID = 0;

            // Get ticket count
            _sqlConnection.Open();
            var command = _sqlConnection.CreateCommand();
            command.CommandText = "SELECT Count(number) FROM Computation.tickets WHERE group_id = @group && ballotBox = @ballotBox";
            command.Parameters.AddWithValue("@group", _configuration.group);
            command.Parameters.AddWithValue("@ballotBox", comboBox_ballotBox.SelectedIndex);
            command.Prepare();
            var result = command.ExecuteReader();

            if (result.Read())
            {
                numberID = int.Parse(result[0].ToString());
                numberID++;
                _sqlConnection.Close();

                var ticketString = "Ticket Nummer: " + _configuration.group + " - " + comboBox_ballotBox.SelectedIndex + " - " + numberID + "\n (Abbrechen erstellt kein Ticket)";

                var messageResult = MessageBox.Show(ticketString, "Ticket", MessageBoxButton.OKCancel, MessageBoxImage.Information);
                if (messageResult.Equals(MessageBoxResult.OK))
                {
                    _sqlConnection.Open();
                    command = _sqlConnection.CreateCommand();
                    command.CommandText = "INSERT INTO tickets (open, group_id, ballotBox, number) VALUES ('1', @group, @ballotBox, @number)";
                    command.Parameters.AddWithValue("@group", _configuration.group);
                    command.Parameters.AddWithValue("@ballotBox", comboBox_ballotBox.SelectedIndex);
                    command.Parameters.AddWithValue("@number", numberID);
                    command.Prepare();

                    command.ExecuteNonQuery();
                    _sqlConnection.Close();
                }

            }
            // Clean
            textBox_firstVote.Text = string.Empty;
            textBox_secondVote.Text = string.Empty;
            textBox_firstVoteList.Text = string.Empty;

            textBox_firstVote.Focus();

            // Should be already
            _sqlConnection.Close();
        }

        private void EnableVote(object sender, SelectionChangedEventArgs e)
        {
            textBox_firstVote.IsEnabled = true;
            textBox_secondVote.IsEnabled = true;
            button_unclear.IsEnabled = true;
            button_confirm.IsEnabled = true;

            textBox_firstVote.Focus();
        }

        private void enterVote(object sender, RoutedEventArgs e)
        {
            if (textBox_firstVoteList.Text == string.Empty || textBox_secondVoteList.Text == string.Empty)
            {
                MessageBox.Show("Ungültige Eingabe", "Fehler", MessageBoxButton.OK, MessageBoxImage.Information);
                textBox_firstVote.Focus();
                return;
            }

            if ((textBox_firstVote.Text == "999" || textBox_secondVote.Text == "999") && !_configuration.wa)
            {
                CreateTicket();
                return;
            }


            if (textBox_firstVote.Text != string.Empty && textBox_secondVote.Text != string.Empty)
            {
                // Frist Vote
                _sqlConnection.Open();
                var command = _sqlConnection.CreateCommand();
                command.CommandText = "INSERT INTO firstVotes (ldfnr, ballotBox) VALUES (@lfdnr, @ballotBox)";
                command.Parameters.AddWithValue("@lfdnr", textBox_firstVote.Text);
                command.Parameters.AddWithValue("@ballotBox", comboBox_ballotBox.SelectedIndex);
                command.Prepare();

                command.ExecuteNonQuery();
                _sqlConnection.Close();

                // Second Vote
                _sqlConnection.Open();
                command = _sqlConnection.CreateCommand();
                command.CommandText = "INSERT INTO secondVotes (ldfnr, ballotBox) VALUES (@lfdnr, @ballotBox)";
                command.Parameters.AddWithValue("@lfdnr", textBox_secondVote.Text);
                command.Parameters.AddWithValue("@ballotBox", comboBox_ballotBox.SelectedIndex);
                command.Prepare();

                command.ExecuteNonQuery();

                command.Parameters.Add(new MySqlParameter("newId", command.LastInsertedId));

                // Log
                var item = new DataListView()
                {
                    Number = Convert.ToInt32(command.Parameters["@newId"].Value),
                    FirstVote = textBox_firstVoteList.Text,
                    SecondVote = textBox_secondVoteName.Text + Space + textBox_secondVoteList.Text
                };

                 _sqlConnection.Close();

                listView_history.Items.Insert(0, item);

                // Avoid huge list of junk data
                if (listView_history.Items.Count >= 50)
                {
                    listView_history.Items.RemoveAt(49);
                }

                // Clean
                textBox_firstVote.Text = string.Empty;
                textBox_secondVote.Text = string.Empty;
                textBox_firstVoteList.Text = string.Empty;

                textBox_firstVote.Focus();
            }
            else
            {
                MessageBox.Show("Keine Eingabe", "Fehler", MessageBoxButton.OK, MessageBoxImage.Information);
                textBox_firstVote.Focus();
            }
        }

        private void exportToCsv(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            ComboBoxItem typeItem = (ComboBoxItem)comboBox_ballotBoxOverview.SelectedItem;

            dlg.FileName = typeItem.Content.ToString().Replace("/","-") + ".csv";
            dlg.DefaultExt = ".csv"; 
            dlg.Filter = "csv Dokumente (.csv)|*.csv";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                StringBuilder sb = new StringBuilder();
                var tmp = "Erstimmen von Wahlurne: " + typeItem.Content.ToString();
                sb.AppendLine(tmp);
                foreach (var item in listView_fristVotesEvaluation.Items)
                {
                    var cast = (DataVotes)item;
                    sb.AppendLine(cast.List + ";" + cast.Votes);
                }

                sb.AppendLine();
                sb.AppendLine();

                sb.AppendLine("Zweitstimmen von Wahlurne: " + typeItem.Content.ToString());
                foreach (var item in listView_secondVotesEvaluation.Items)
                {
                    var cast = (DataVotes)item;
                    sb.AppendLine(cast.List + ";" + cast.Name + ";" + cast.Votes);
                }
                File.WriteAllText(dlg.FileName, sb.ToString());
            }
        }

        private void LoadConfiguration()
        {
            try
            {
                XmlSerializer serialiser = new XmlSerializer(typeof(Configuration));
                _configuration = (Configuration)serialiser.Deserialize(File.OpenRead("Configuration.xml"));

                if (!_configuration.wa)
                {
                    ticketTab.Visibility = System.Windows.Visibility.Hidden;
                    tabItem_overview.Visibility = System.Windows.Visibility.Hidden;
                }
                else
                {
                    _ticketTimer.Tick += new EventHandler(updateTickets_Tick);
                    _ticketTimer.Interval = new TimeSpan(0, 0, _configuration.updateTime);
                    _ticketTimer.Start();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Konnte die Konfiguration nicht laden. \n (Configuration.xml vorhanden?)", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(-1);
            }
        }

        private void ProcessOverview(object sender, SelectionChangedEventArgs e)
        {
            UpdateOverView();
        }

        private void selectTicket(object sender, SelectionChangedEventArgs e)
        {
            var item = (DataTicket)listView_tickets.SelectedItem;
            if (item != null)
            {
                textBox_ticketID.Text = item.Id.ToString();
                textBox_ticketGroup.Text = item.GroupId.ToString();
                textBox_ticketBallotBox.Text = item.BallotBox;
                textBox_ticketNumber.Text = item.Number.ToString();
            }
        }

        private void SetUpConnection()
        {
            string conString = "SERVER=" + _configuration.database + ";" +
            "DATABASE=Computation;" +
            "UID=" + _configuration.user +
            ";PASSWORD=" + _configuration.password + ";";

            _sqlConnection = new MySqlConnection(conString);
            try
            {
                _sqlConnection.Open();
                if (!_sqlConnection.Ping())
                {
                    MessageBox.Show("Die Verbindung zur Datenbank konnte nicht hergestellt werden", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                    //Environment.Exit(-1);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Die Verbindung zur Datenbank konnte nicht hergestellt werden", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                //Environment.Exit(-1);
            }
            _sqlConnection.Close();
        }

        private void UpdateOverView()
        {
            switch (comboBox_ballotBoxOverview.SelectedIndex)
            {
                case 9:
                    // First Votes
                    {
                        _sqlConnection.Open();
                        var command = _sqlConnection.CreateCommand();
                        command.CommandText = "SELECT lists.ldfnr, lists.list, count( firstVotes.ldfnr )" +
                                                "FROM firstVotes " +
                                                "INNER JOIN lists ON lists.ldfnr = firstVotes.ldfnr " +
                                                "GROUP BY firstVotes.ldfnr " +
                                                "ORDER BY lists.ldfnr ASC ";
                        var result = command.ExecuteReader();

                        listView_fristVotesEvaluation.Items.Clear();

                        while (result.Read())
                        {
                            var data = new DataVotes()
                            {
                                List = result[1].ToString(),
                                Votes = int.Parse(result[2].ToString())
                            };
                            listView_fristVotesEvaluation.Items.Add(data);
                        }
                        _sqlConnection.Close();
                    }

                    // Second Votes
                    {
                        _sqlConnection.Open();
                        var command = _sqlConnection.CreateCommand();
                        command.CommandText = "SELECT candidates.lfdnr, candidates.name, lists.list, count( secondVotes.ldfnr ) " +
                                                "FROM secondVotes " +
                                                "INNER JOIN candidates ON candidates.lfdnr = secondVotes.ldfnr " +
                                                "INNER JOIN lists ON lists.ldfnr = candidates.list_lfdnr " +
                                                "GROUP BY secondVotes.ldfnr " +
                                                "ORDER BY candidates.lfdnr ASC";
                        var result = command.ExecuteReader();

                        listView_secondVotesEvaluation.Items.Clear();

                        while (result.Read())
                        {
                            var data = new DataVotes()
                            {
                                Name = result[1].ToString(),
                                List = result[2].ToString(),
                                Votes = int.Parse(result[3].ToString())
                            };
                            listView_secondVotesEvaluation.Items.Add(data);
                        }
                        _sqlConnection.Close();
                    }

                    break;
                default:
                    // First Votes
                    {
                        _sqlConnection.Open();
                        var command = _sqlConnection.CreateCommand();
                        command.CommandText = "SELECT lists.ldfnr, lists.list, count( firstVotes.ldfnr )" +
                                                "FROM firstVotes " +
                                                "INNER JOIN lists ON lists.ldfnr = firstVotes.ldfnr " +
                                                "WHERE firstVotes.ballotBox = @ballotBox " +
                                                "GROUP BY firstVotes.ldfnr " +
                                                "ORDER BY lists.ldfnr ASC ";

                        command.Parameters.AddWithValue("@ballotBox", comboBox_ballotBoxOverview.SelectedIndex);
                        command.Prepare();

                        var result = command.ExecuteReader();

                        listView_fristVotesEvaluation.Items.Clear();

                        while (result.Read())
                        {
                            var data = new DataVotes()
                            {
                                List = result[1].ToString(),
                                Votes = int.Parse(result[2].ToString())
                            };
                            listView_fristVotesEvaluation.Items.Add(data);
                        }
                        _sqlConnection.Close();
                    }

                    // Second Votes
                    {
                        _sqlConnection.Open();
                        var command = _sqlConnection.CreateCommand();
                        command.CommandText = "SELECT candidates.lfdnr, candidates.name, lists.list, count( secondVotes.ldfnr ) " +
                                                "FROM secondVotes " +
                                                "INNER JOIN candidates ON candidates.lfdnr = secondVotes.ldfnr " +
                                                "INNER JOIN lists ON lists.ldfnr = candidates.list_lfdnr " +
                                                "WHERE secondVotes.ballotBox = @ballotBox " +
                                                "GROUP BY secondVotes.ldfnr " +
                                                "ORDER BY candidates.lfdnr ASC";
                        command.Parameters.AddWithValue("@ballotBox", comboBox_ballotBoxOverview.SelectedIndex);
                        command.Prepare();

                        var result = command.ExecuteReader();

                        listView_secondVotesEvaluation.Items.Clear();

                        while (result.Read())
                        {
                            var data = new DataVotes()
                            {
                                Name = result[1].ToString(),
                                List = result[2].ToString(),
                                Votes = int.Parse(result[3].ToString())
                            };
                            listView_secondVotesEvaluation.Items.Add(data);
                        }
                        _sqlConnection.Close();
                    }
                    break;
            }
        }

        private void updateResults(object sender, RoutedEventArgs e)
        {
            UpdateOverView();
        }

        private void updateTickets(object sender, RoutedEventArgs e)
        {
            updateTicketsLogic();
        }

        private void updateTickets_Tick(object sender, EventArgs e)
        {
            try
            {
                updateTicketsLogic();

            }
            catch (Exception)
            {
                // in this moment the user interacts with the db
            }
            _sqlConnection.Close();
        }

        private void updateTicketsLogic()
        {
            _sqlConnection.Open();
            var command = _sqlConnection.CreateCommand();
            command.CommandText = "SELECT Id, Group_Id, BallotBox, Number FROM OpenTicketsView";
            var result = command.ExecuteReader();

            listView_tickets.Items.Clear();

            while (result.Read())
            {
                var data = new DataTicket()
                {
                    Id = int.Parse(result[0].ToString()),
                    GroupId = int.Parse(result[1].ToString()),
                    BallotBox = result[2].ToString(),
                    Number = int.Parse(result[3].ToString())
                };
                listView_tickets.Items.Add(data);
            }
            _sqlConnection.Close();
        }
    }
}
