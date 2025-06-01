using ChristmasLibrary;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ChristmasThing
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private const int _historicalDuplicationInYears = 2;
        public ObservableCollection<ParticipantViewModel> Participants { get; } = new ObservableCollection<ParticipantViewModel>();
        public ObservableCollection<ParticipantViewModel> SelectedParticipants { get; } = new ObservableCollection<ParticipantViewModel>();

        public ObservableCollection<int> AvailableGiftExchangeYears { get; } = new ObservableCollection<int>();
        public ObservableCollection<ParticipantViewModel> AvailableSignificantOthers { get; } = new ObservableCollection<ParticipantViewModel>();
        private Dictionary<int, List<GiftExchangeViewModel>> _historicalGiftExchanges = new Dictionary<int, List<GiftExchangeViewModel>>();
        public ObservableCollection<SecretSantaExchangeViewModel> NewSecretSantaExchanges { get; } = new ObservableCollection<SecretSantaExchangeViewModel>();
        public Canvas HistoricalGiftExchangeCanvas { get; set; } = new Canvas { Height = 600, Width = 800 };
        public Canvas NewGiftExchangeCanvas { get; set; } = new Canvas { Height = 600, Width = 800 };
        private readonly SqlConnection _connection;

        private void CreateNewGiftExchange(IList<ParticipantViewModel> participants)
        {
            NewSecretSantaExchanges.Clear();

            var orderedParticipants = participants.OrderBy(p => p.Name).ToList();

            var year = 2025; //DateTime.Now.Year;
            var list = new List<GiftExchangeViewModel>();
            var recentGiftExchanges = GetRecentGiftExchanges(_historicalDuplicationInYears);

            var listOfValidExchanges = new List<GiftExchangeViewModel>();
            foreach (var participant in orderedParticipants)
            {
                var listOfRecentReceivers = recentGiftExchanges.Where(e => e.GiverId == participant.Id).Select(e => e.ReceiverId);
                var listOfValidReceivers = orderedParticipants.Where(p => p.Id != participant.Id && p.Id != participant.SignificantOtherId && !listOfRecentReceivers.Contains(p.Id));

                foreach (var receiver in listOfValidReceivers)
                {
                    listOfValidExchanges.Add(new GiftExchangeViewModel(new GiftExchange { Year = year, GiverId = participant.Id, ReceiverId = receiver.Id }) { GiverName = participant.Name, ReceiverName = receiver.Name });
                }
            }

            CreateValidGiftExchanges(orderedParticipants, listOfValidExchanges, list, orderedParticipants.First().Id, orderedParticipants.First().Id);
        }

        private void CreateValidGiftExchanges(IList<ParticipantViewModel> participants, List<GiftExchangeViewModel> validExchanges, List<GiftExchangeViewModel> list, Guid giverId, Guid endingReceiverId)
        {
            var participant = participants.FirstOrDefault(p => p.Id == giverId);
            if (participant is null)
            {
                return;
            }

            var valids = validExchanges.Where(e => e.GiverId == giverId);

            foreach (var giftExchange in valids)
            {
                if (list.Count == participants.Count - 1)
                {
                    // looking for final
                    if (endingReceiverId == giftExchange.ReceiverId && !list.Any(e => e.ReceiverId == giftExchange.ReceiverId))
                    {
                        list.Add(giftExchange);

                        // add to list
                        NewSecretSantaExchanges.Add(new SecretSantaExchangeViewModel(list, NewSecretSantaExchanges.Count + 1));
                        list.Remove(giftExchange);
                        return;
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    if ((list.Count == 0 || endingReceiverId != giftExchange.ReceiverId) && IsAbleToBeAdded(list, giftExchange))
                    {
                        list.Add(giftExchange);

                        CreateValidGiftExchanges(participants.ToList(), validExchanges, list.ToList(), giftExchange.ReceiverId, endingReceiverId);

                        list.Remove(giftExchange);
                    }
                    else
                    {
                        continue;
                    }
                }
            }
        }

        private bool IsAbleToBeAdded(List<GiftExchangeViewModel> list, GiftExchangeViewModel giftExchange)
        {
            bool duplicateGiver = list.Any(e => e.GiverId == giftExchange.GiverId);
            bool duplicateReceiver = list.Any(e => e.ReceiverId == giftExchange.ReceiverId);
            bool earlyClosure = list.Any(e => e.GiverId == giftExchange.ReceiverId);

            return !duplicateGiver && !duplicateReceiver;
        }

        private IList<GiftExchangeViewModel> GetRecentGiftExchanges(int historyToGet)
        {
            var list = new List<GiftExchangeViewModel>();
            var year = 2025; //DateTime.Now.Year;
            int count = 0;
            int i = 1;

            while (count < historyToGet)
            {
                if (_historicalGiftExchanges.ContainsKey(year - i))
                {
                    list.AddRange(_historicalGiftExchanges[year - i].ToList());
                    count++;
                }

                if (year - i < _historicalGiftExchanges.Keys.Min())
                {
                    break;
                }

                i++;
            }

            return list;
        }

        private void Initialize()
        {
            AvailableSignificantOthers?.Clear();
            Participants?.Clear();
            AvailableGiftExchangeYears?.Clear();
            _historicalGiftExchanges?.Clear();

            var participants = LoadParticipants();

            AvailableSignificantOthers.Add(new ParticipantViewModel(Participant.Empty));
            foreach (var participant in participants)
            {
                var p = new ParticipantViewModel(participant);
                Participants.Add(p);
                AvailableSignificantOthers.Add(p);
            }

            var giftExchanges = LoadGiftExchanges();
            foreach (var giftExchange in giftExchanges)
            {
                if (!_historicalGiftExchanges.ContainsKey(giftExchange.Year))
                {
                    AvailableGiftExchangeYears.Add(giftExchange.Year);
                    _historicalGiftExchanges.Add(giftExchange.Year, new List<GiftExchangeViewModel>());
                }

                var g = new GiftExchangeViewModel(giftExchange)
                {
                    GiverName = Participants.First(p => p.Id == giftExchange.GiverId).Name,
                    ReceiverName = Participants.First(p => p.Id == giftExchange.ReceiverId).Name,
                };
                _historicalGiftExchanges[giftExchange.Year].Add(g);
            }
        }

        public MainWindowViewModel() 
        {
            string directory = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            _connection = new SqlConnection(@$"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={directory}\SecretSanta.mdf;Integrated Security=True;Connect Timeout=30");

            Initialize();

            AddNewParticipantCommand = new DelegateCommand(AddNewParticipantCommandExecute, AddNewParticipantCommandCanExecute);
            SaveEditedParticipantCommand = new DelegateCommand(SaveEditedParticipantCommandExecute, SaveEditedParticipantCommandCanExecute);
            CreateNewGiftExchangeCommand = new DelegateCommand(CreateNewGiftExchangeCommandExecute, CreateNewGiftExchangeCommandCanExecute);
            SaveNewGiftExchangeCommand = new DelegateCommand(SaveNewGiftExchangeCommandExecute, SaveNewGiftExchangeCommandCanExecute);
        }

        private static Path DrawLinkArrow(Point p1, Point p2)
        {
            GeometryGroup lineGroup = new GeometryGroup();
            double theta = Math.Atan2((p2.Y - p1.Y), (p2.X - p1.X)) * 180 / Math.PI;

            PathGeometry pathGeometry = new PathGeometry();
            PathFigure pathFigure = new PathFigure();
            Point p = new Point(p1.X + ((p2.X - p1.X) / 1), p1.Y + ((p2.Y - p1.Y) / 1));
            pathFigure.StartPoint = p;

            Point lpoint = new Point(p.X + 6, p.Y + 15);
            Point rpoint = new Point(p.X - 6, p.Y + 15);
            LineSegment seg1 = new LineSegment
            {
                Point = lpoint
            };
            pathFigure.Segments.Add(seg1);

            LineSegment seg2 = new LineSegment
            {
                Point = rpoint
            };
            pathFigure.Segments.Add(seg2);

            LineSegment seg3 = new LineSegment();
            seg3.Point = p;
            pathFigure.Segments.Add(seg3);

            pathGeometry.Figures.Add(pathFigure);
            RotateTransform transform = new RotateTransform
            {
                Angle = theta + 90,
                CenterX = p.X,
                CenterY = p.Y
            };
            pathGeometry.Transform = transform;
            lineGroup.Children.Add(pathGeometry);

            LineGeometry connectorGeometry = new LineGeometry
            {
                StartPoint = p1,
                EndPoint = p2
            };
            lineGroup.Children.Add(connectorGeometry);

            Path path = new Path
            {
                Data = lineGroup,
                StrokeThickness = 2,
                Stroke = Brushes.Black,
                Fill = Brushes.Black,
            };

            return path;
        }

        private static int DetermineCycleCount(IList<GiftExchangeViewModel> giftExchanges)
        {
            var retVal = 0;
            var giverId = giftExchanges[0].GiverId;

            while (true)
            {
                var exchange = giftExchanges.FirstOrDefault(e => e.GiverId == giverId);

                if (exchange is null)
                {
                    retVal++;

                    if (!giftExchanges.Any())
                    {
                        break;
                    }

                    exchange = giftExchanges.First();
                }

                giftExchanges.Remove(exchange);

                giverId = exchange.ReceiverId;
            }

            return retVal;
        }

        private void UpdateCanvas(IList<GiftExchangeViewModel> giftExchanges, Canvas canvas)
        {
            canvas.Children.Clear();
            var cycles = DetermineCycleCount(giftExchanges.ToList());

            var numberOfRectangles = giftExchanges.Count + cycles;
            var numberOfLines = numberOfRectangles - 1;
            var lineWidth = 800 / ((numberOfRectangles * 1.5) + numberOfLines);
            var rectangleWidth = lineWidth * 1.5;

            var giverId = giftExchanges[0].GiverId;
            var giverName = giftExchanges[0].GiverName;

            var grid1 = new Grid();
            grid1.Children.Add(new Rectangle { Stroke = Brushes.Black, Width = rectangleWidth, Height = 50 });
            grid1.Children.Add(new TextBlock { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Text = giverName });

            Canvas.SetLeft(grid1, 0);
            Canvas.SetTop(grid1, 250);
            canvas.Children.Add(grid1);

            var currentPos = rectangleWidth;

            while (true)
            {
                var exchange = giftExchanges.FirstOrDefault(e => e.GiverId == giverId);

                if (exchange is null)
                {
                    if (!giftExchanges.Any())
                    {
                        return;
                    }

                    currentPos += lineWidth;

                    exchange = giftExchanges.First();

                    var grid2 = new Grid();
                    grid2.Children.Add(new Rectangle { Stroke = Brushes.Black, Width = rectangleWidth, Height = 50 });
                    grid2.Children.Add(new TextBlock { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Text = exchange.GiverName });

                    Canvas.SetLeft(grid2, currentPos);
                    Canvas.SetTop(grid2, 250);
                    canvas.Children.Add(grid2);

                    currentPos += rectangleWidth;
                }

                giftExchanges.Remove(exchange);

                giverId = exchange.ReceiverId;

                canvas.Children.Add(DrawLinkArrow(new Point(currentPos, 275), new Point(currentPos + lineWidth, 275)));

                currentPos += lineWidth;

                var grid = new Grid();
                grid.Children.Add(new Rectangle { Stroke = Brushes.Black, Width = rectangleWidth, Height = 50 });
                grid.Children.Add(new TextBlock { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Text = exchange.ReceiverName });

                Canvas.SetLeft(grid, currentPos);
                Canvas.SetTop(grid, 250);
                canvas.Children.Add(grid);

                currentPos += rectangleWidth;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private string _newParticipantName = string.Empty;
        public string NewParticipantName
        {
            get => _newParticipantName;
            set
            {
                _newParticipantName = value;
                OnPropertyChanged(nameof(NewParticipantName));
            }
        }

        private ParticipantViewModel _selectedParticipantToEdit;
        public ParticipantViewModel? SelectedParticipantToEdit
        {
            get => _selectedParticipantToEdit;
            set
            {
                if(_selectedParticipantToEdit != value)
                {
                    _selectedParticipantToEdit = value;

                    if(_selectedParticipantToEdit is not null)
                    {
                        SelectedParticipantToEditNewName = _selectedParticipantToEdit.Name;
                        SelectedParticipantToEditNewSigOther = AvailableSignificantOthers?.FirstOrDefault(s => s.Id == _selectedParticipantToEdit.SignificantOtherId);
                    }
                }
                OnPropertyChanged(nameof(SelectedParticipantToEdit));
            }
        }

        private string _selectedParticipantToEditNewName;
        public string? SelectedParticipantToEditNewName
        {
            get => _selectedParticipantToEditNewName;
            set
            {
                _selectedParticipantToEditNewName = value;
                OnPropertyChanged(nameof(SelectedParticipantToEditNewName));
            }
        }

        private ParticipantViewModel? _selectedParticipantToEditNewSigOther;
        public ParticipantViewModel? SelectedParticipantToEditNewSigOther
        {
            get => _selectedParticipantToEditNewSigOther;
            set
            {
                _selectedParticipantToEditNewSigOther = value;
                OnPropertyChanged(nameof(SelectedParticipantToEditNewSigOther));
            }
        }

        private SecretSantaExchangeViewModel _selectedSecretSantaExchange;
        public SecretSantaExchangeViewModel SelectedSecretSantaExchange
        {
            get => _selectedSecretSantaExchange;
            set
            {
                _selectedSecretSantaExchange = value;
                OnPropertyChanged(nameof(SelectedSecretSantaExchange));

                if (_selectedSecretSantaExchange is not null)
                {
                    UpdateCanvas(_selectedSecretSantaExchange.GiftExchanges.OrderBy(e => e.GiverName).ToList(), NewGiftExchangeCanvas);
                }
                else
                {
                    NewGiftExchangeCanvas?.Children?.Clear();
                }
            }
        }

        private int _selectedHistoricalGiftExchangeYear;

        public int SelectedHistoricalGiftExchangeYear
        {
            get => _selectedHistoricalGiftExchangeYear;
            set
            {
                _selectedHistoricalGiftExchangeYear = value;
                OnPropertyChanged(nameof(SelectedHistoricalGiftExchangeYear));

                if (_historicalGiftExchanges.ContainsKey(SelectedHistoricalGiftExchangeYear))
                {
                    UpdateCanvas(_historicalGiftExchanges[SelectedHistoricalGiftExchangeYear].OrderBy(e => e.GiverName).ToList(), HistoricalGiftExchangeCanvas);
                }
            }
        }

        private bool SaveNewGiftExchangeCommandCanExecute(object obj)
        {
            return true;// !string.IsNullOrWhiteSpace(NewParticipantName);
        }

        private void SaveNewGiftExchangeCommandExecute(object obj)
        {
            foreach (var giftExchange in SelectedSecretSantaExchange.GiftExchanges.ToList())
            {
                AddGiftExchange(giftExchange);
            }
        }

        public ICommand SaveNewGiftExchangeCommand { get; set; }

        private bool CreateNewGiftExchangeCommandCanExecute(object obj)
        {
            return true;// !string.IsNullOrWhiteSpace(NewParticipantName);
        }

        private void CreateNewGiftExchangeCommandExecute(object obj)
        {
            CreateNewGiftExchange(SelectedParticipants.ToList());
        }

        public ICommand CreateNewGiftExchangeCommand { get; set; }

        private bool AddNewParticipantCommandCanExecute(object obj)
        {
            return true;// !string.IsNullOrWhiteSpace(NewParticipantName);
        }

        private void AddNewParticipantCommandExecute(object obj)
        {
            AddParticipant(Guid.NewGuid(), NewParticipantName, null);
        }

        public ICommand AddNewParticipantCommand { get; set; }

        private bool SaveEditedParticipantCommandCanExecute(object obj)
        {
            return SelectedParticipantToEdit is not null
                && 
                    (SelectedParticipantToEditNewName != SelectedParticipantToEdit.Name
                    || SelectedParticipantToEditNewSigOther?.Id != SelectedParticipantToEdit.SignificantOtherId);
        }

        private void SaveEditedParticipantCommandExecute(object obj)
        {
            var newSigOtherId = SelectedParticipantToEditNewSigOther?.Id;
            var oldSigOtherId = SelectedParticipantToEdit?.SignificantOtherId;
            if (newSigOtherId != oldSigOtherId) // if sigIds dont match,
            {
                if (oldSigOtherId.HasValue)
                {
                    // need to clear the old sig other's sigId if there was one
                    UpdateSigOther(oldSigOtherId.Value);
                }

                if (newSigOtherId.HasValue)
                {
                    // need to update the new sig other's sigId
                    UpdateSigOther(newSigOtherId.Value, SelectedParticipantToEdit.Id);
                }
            }

            SaveParticipant(SelectedParticipantToEdit.Id, SelectedParticipantToEditNewName ?? SelectedParticipantToEdit.Name, SelectedParticipantToEditNewSigOther?.Id ?? SelectedParticipantToEdit.SignificantOtherId);
        }

        public ICommand SaveEditedParticipantCommand { get; set; }

        private IList<GiftExchange> LoadGiftExchanges()
        {
            _connection.Open();
            var giftExchanges = new List<GiftExchange>();

            var command = new SqlCommand("select * from GiftExchange", _connection);
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                int year = int.Parse(reader["Year"].ToString());
                string giverIdString = reader["GiverId"].ToString();
                string receiverIdString = reader["ReceiverId"].ToString();

                Guid giverId = Guid.Parse(giverIdString);
                Guid receiverId = Guid.Parse(receiverIdString);

                giftExchanges.Add(new GiftExchange { Year = year, GiverId = giverId, ReceiverId = receiverId });
            }

            _connection.Close();

            return giftExchanges;
        }

        private IList<Participant> LoadParticipants()
        {
            _connection.Open();
            var participants = new List<Participant>();

            var command = new SqlCommand("select * from Participant", _connection);
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                string name = reader["Name"].ToString();
                string idString = reader["Id"].ToString();
                string soString = reader["SignificantOtherId"]?.ToString();

                Guid id = Guid.Parse(idString);
                Guid? significantOtherId = string.IsNullOrWhiteSpace(soString) ? null : Guid.Parse(soString);

                participants.Add(new Participant { Id = id, Name = name, SignificantOtherId = significantOtherId });
            }

            _connection.Close();

            return participants;
        }

        private void SaveParticipant(Guid id, string name, Guid? significantOtherId)
        {
            _connection.Open();

            var command = new SqlCommand($"update Participant SET Name = @Name, SignificantOtherId = @SoId WHERE Id = @Id", _connection);
            command.Parameters.AddWithValue("@Id", id.ToString());
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@SoId", significantOtherId?.ToString() ?? null);

            command.ExecuteNonQuery();

            _connection.Close();

            SelectedParticipantToEdit = null;
            SelectedParticipantToEditNewName = null;
            SelectedParticipantToEditNewSigOther = null;

            Initialize();
        }

        private void UpdateSigOther(Guid id, Guid significantOtherId)
        {
            _connection.Open();

            var command = new SqlCommand($"update Participant SET SignificantOtherId = @SoId WHERE Id = @Id", _connection);
            command.Parameters.AddWithValue("@Id", id.ToString());
            command.Parameters.AddWithValue("@SoId", significantOtherId.ToString());

            command.ExecuteNonQuery();

            _connection.Close();
        }

        private void UpdateSigOther(Guid id)
        {
            _connection.Open();

            var command = new SqlCommand($"update Participant SET SignificantOtherId = NULL WHERE Id = @Id", _connection);
            command.Parameters.AddWithValue("@Id", id.ToString());

            command.ExecuteNonQuery();

            _connection.Close();
        }

        private void AddParticipant(Guid id, string name, Guid? significantOtherId)
        {
            _connection.Open();

            var command = new SqlCommand($"insert into Participant (Id, Name, SignificantOtherId) values (@Id, @Name, @SoId)", _connection);
            command.Parameters.AddWithValue("@Id", id.ToString());
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@SoId", significantOtherId?.ToString() ?? null);

            command.ExecuteNonQuery();

            _connection.Close();
        }

        private void AddGiftExchange(GiftExchangeViewModel giftExchange)
        {
            _connection.Open();

            var command = new SqlCommand($"insert into GiftExchange (Year, GiverId, ReceiverId) values (@Year, @GiverId, @ReceiverId)", _connection);
            command.Parameters.AddWithValue("@Year", giftExchange.Year);
            command.Parameters.AddWithValue("@GiverId", giftExchange.GiverId.ToString());
            command.Parameters.AddWithValue("@ReceiverId", giftExchange.ReceiverId.ToString());

            command.ExecuteNonQuery();

            _connection.Close();
        }
    }
}