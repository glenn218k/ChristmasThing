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
        public ObservableCollection<ParticipantViewModel> Participants { get; } = new ObservableCollection<ParticipantViewModel>();
        public ObservableCollection<ParticipantViewModel> SelectedParticipants { get; } = new ObservableCollection<ParticipantViewModel>();
        public ObservableCollection<int> AvailableGiftExchangeYears { get; } = new ObservableCollection<int>();
        public ObservableCollection<ParticipantViewModel> AvailableSignificantOthers { get; } = new ObservableCollection<ParticipantViewModel>();
        private Dictionary<int, List<GiftExchangeViewModel>> _giftExchanges = new Dictionary<int, List<GiftExchangeViewModel>>();

        private IList<GiftExchangeViewModel> _newGiftExchanges = new List<GiftExchangeViewModel>();

        public Canvas HistoricalGiftExchangeCanvas { get; set; } = new Canvas { Height = 600, Width = 800 };
        public Canvas NewGiftExchangeCanvas { get; set; } = new Canvas { Height = 600, Width = 800 };

        private readonly SqlConnection _connection;

        private IList<GiftExchangeViewModel> CreateNewGiftExchange(IList<ParticipantViewModel> participants)
        {
            var year = DateTime.Now.Year;
            var list = new List<GiftExchangeViewModel>();
            var recentGiftExchanges = GetRecentGiftExchanges(2);

            var listOfValidExchanges = new List<GiftExchangeViewModel>();
            foreach (var participant in participants)
            {
                var listOfRecentReceivers = recentGiftExchanges.Where(e => e.GiverId == participant.Id).Select(e => e.ReceiverId);
                var listOfValidReceivers = participants.Where(p => p.Id != participant.Id && p.Id != participant.SignificantOtherId && !listOfRecentReceivers.Contains(p.Id));

                foreach (var receiver in listOfValidReceivers)
                {
                    listOfValidExchanges.Add(new GiftExchangeViewModel(new GiftExchange { Year = year, GiverId = participant.Id, ReceiverId = receiver.Id }) { GiverName = participant.Name, ReceiverName = receiver.Name });
                }
            }

            GetValid(participants.ToList(), listOfValidExchanges, list, participants.First().Id, participants.First().Id);

            return list;
        }

        private bool GetValid(IList<ParticipantViewModel> participants, List<GiftExchangeViewModel> validExchanges, List<GiftExchangeViewModel> list, Guid giverId, Guid endingReceiverId)
        {
            var participant = participants.FirstOrDefault(p => p.Id == giverId);
            if (participant is null)
            {
                return false;
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

                        return true;
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    if ((list.Count == 0 || endingReceiverId != giftExchange.ReceiverId) && !list.Any(e => e.ReceiverId == giftExchange.ReceiverId && e.GiverId != giftExchange.ReceiverId))
                    {
                        list.Add(giftExchange);

                        if (GetValid(participants.ToList(), validExchanges, list, giftExchange.ReceiverId, endingReceiverId))
                        {
                            return true;
                        }
                        else
                        {
                            list.Remove(giftExchange);
                        }
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            return false;
        }

        private IList<GiftExchangeViewModel> GetRecentGiftExchanges(int historyToGet)
        {
            var list = new List<GiftExchangeViewModel>();
            var year = DateTime.Now.Year;
            int count = 0;
            int i = 1;

            while (count < historyToGet)
            {
                if (_giftExchanges.ContainsKey(year - i))
                {
                    list.AddRange(_giftExchanges[year - i].ToList());
                    count++;
                }

                if (year - i < _giftExchanges.Keys.Min())
                {
                    break;
                }

                i++;
            }

            return list;
        }

        public MainWindowViewModel() 
        {
            string directory = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            _connection = new SqlConnection(@$"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={directory}\SecretSanta.mdf;Integrated Security=True;Connect Timeout=30");

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
                if (!_giftExchanges.ContainsKey(giftExchange.Year))
                {
                    AvailableGiftExchangeYears.Add(giftExchange.Year);
                    _giftExchanges.Add(giftExchange.Year, new List<GiftExchangeViewModel>());
                }

                var g = new GiftExchangeViewModel(giftExchange)
                {
                    GiverName = Participants.First(p => p.Id == giftExchange.GiverId).Name,
                    ReceiverName = Participants.First(p => p.Id == giftExchange.ReceiverId).Name,
                };
                _giftExchanges[giftExchange.Year].Add(g);
            }

            AddNewParticipantCommand = new DelegateCommand(AddNewParticipantCommandExecute, AddNewParticipantCommandCanExecute);
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

        private string _newGiftExchangeDisplayText = string.Empty;
        public string NewGiftExchangeDisplayText
        {
            get => _newGiftExchangeDisplayText;
            set
            {
                _newGiftExchangeDisplayText = value;
                OnPropertyChanged(nameof(NewGiftExchangeDisplayText));
            }
        }

        private int _selectedGiftExchangeYear;
        public int SelectedGiftExchangeYear
        {
            get => _selectedGiftExchangeYear;
            set
            {
                _selectedGiftExchangeYear = value;
                OnPropertyChanged(nameof(SelectedGiftExchangeYear));

                if (_giftExchanges.ContainsKey(SelectedGiftExchangeYear))
                {
                    UpdateCanvas(_giftExchanges[SelectedGiftExchangeYear].OrderBy(e => e.GiverName).ToList(), HistoricalGiftExchangeCanvas);
                }
            }
        }

        private bool SaveNewGiftExchangeCommandCanExecute(object obj)
        {
            return true;// !string.IsNullOrWhiteSpace(NewParticipantName);
        }

        private void SaveNewGiftExchangeCommandExecute(object obj)
        {
            foreach (var giftExchange in _newGiftExchanges)
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
            _newGiftExchanges = CreateNewGiftExchange(SelectedParticipants.ToList());

            NewGiftExchangeDisplayText = string.Join(Environment.NewLine, _newGiftExchanges.Select(a => a.ToString()));

            UpdateCanvas(_newGiftExchanges.OrderBy(e => e.GiverName).ToList(), NewGiftExchangeCanvas);
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