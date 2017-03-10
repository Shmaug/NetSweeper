using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Shapes;

namespace NetSweeper {
    public partial class MainWindow : Window {
        SolidColorBrush lightRedBrush = new SolidColorBrush(Color.FromRgb(101, 69, 69));
        SolidColorBrush redBrush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
        SolidColorBrush blueBrush = new SolidColorBrush(Color.FromRgb(0, 0, 255));
        SolidColorBrush yellowBrush = new SolidColorBrush(Color.FromRgb(255, 255, 0));
        SolidColorBrush blackBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        SolidColorBrush grayBrush = new SolidColorBrush(Color.FromRgb(127, 127, 127));
        SolidColorBrush whiteBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));

        List<Rectangle> rectangles = new List<Rectangle>();
        List<Line> lines = new List<Line>();

        Label[,] gameLabelGrid;
        Label[,] inputLabelGrid;
        Label[,] outputLabelGrid;
        DispatcherTimer timer;

        public MainWindow() {
            InitializeComponent();
        }

        private void Timer_Tick(object sender, EventArgs e) {
            try {
                NetworkController.Update();
            } catch (Exception ex) {
                if (MessageBox.Show(ex.StackTrace, ex.Message, MessageBoxButton.OKCancel, MessageBoxImage.Error) == MessageBoxResult.Cancel)
                    Close();
            }
            Redraw();
        }

        void SetupGrid() {
            gameGrid.Columns = NetworkController.game.gameSize;
            gameGrid.Rows = NetworkController.game.gameSize;

            gameLabelGrid = new Label[NetworkController.game.gameSize, NetworkController.game.gameSize];
            inputLabelGrid = new Label[NetworkController.game.gameSize, NetworkController.game.gameSize];
            outputLabelGrid = new Label[NetworkController.game.gameSize, NetworkController.game.gameSize];

            double gw = gameGrid.Width / NetworkController.game.gameSize;
            double gh = gameGrid.Height / NetworkController.game.gameSize;

            double iow = inputGrid.Width / NetworkController.game.gameSize;
            double ioh = inputGrid.Height / NetworkController.game.gameSize;
            for (int x = 0; x < NetworkController.game.gameSize; x++)
                for (int y = 0; y < NetworkController.game.gameSize; y++) {
                    gameLabelGrid[x, y] = new Label();
                    gameLabelGrid[x, y].Width = gw;
                    gameLabelGrid[x, y].Height = gh;
                    gameLabelGrid[x, y].BorderBrush = blackBrush;
                    gameLabelGrid[x, y].BorderThickness = new Thickness(1, 1, 1, 1);
                    gameLabelGrid[x, y].Background = grayBrush;
                    gameLabelGrid[x, y].HorizontalContentAlignment = HorizontalAlignment.Center;
                    gameLabelGrid[x, y].VerticalContentAlignment = VerticalAlignment.Center;
                    gameLabelGrid[x, y].FontSize = 14;
                    gameLabelGrid[x, y].Tag = new Point(x, y);
                    gameGrid.Children.Add(gameLabelGrid[x, y]);

                    inputLabelGrid[x, y] = new Label();
                    inputLabelGrid[x, y].Width = iow;
                    inputLabelGrid[x, y].Height = ioh;
                    inputLabelGrid[x, y].BorderBrush = blackBrush;
                    inputLabelGrid[x, y].BorderThickness = new Thickness(1, 1, 1, 1);
                    inputLabelGrid[x, y].Background = grayBrush;
                    inputLabelGrid[x, y].HorizontalContentAlignment = HorizontalAlignment.Center;
                    inputLabelGrid[x, y].VerticalContentAlignment = VerticalAlignment.Center;
                    inputLabelGrid[x, y].FontSize = 6;
                    inputLabelGrid[x, y].Tag = new Point(x, y);
                    inputGrid.Children.Add(inputLabelGrid[x, y]);

                    outputLabelGrid[x, y] = new Label();
                    outputLabelGrid[x, y].Width = iow;
                    outputLabelGrid[x, y].Height = ioh;
                    outputLabelGrid[x, y].BorderBrush = blackBrush;
                    outputLabelGrid[x, y].BorderThickness = new Thickness(1, 1, 1, 1);
                    outputLabelGrid[x, y].Background = grayBrush;
                    outputLabelGrid[x, y].HorizontalContentAlignment = HorizontalAlignment.Center;
                    outputLabelGrid[x, y].VerticalContentAlignment = VerticalAlignment.Center;
                    outputLabelGrid[x, y].FontSize = 6;
                    outputLabelGrid[x, y].Tag = new Point(x, y);
                    outputGrid.Children.Add(outputLabelGrid[x, y]);
                }
        }
        
        Rectangle getRect() {
            Rectangle r = new Rectangle();
            r.Width = biasCell.Width;
            r.Height = biasCell.Height;
            r.Fill = biasCell.Fill;
            r.HorizontalAlignment = HorizontalAlignment.Left;
            r.VerticalAlignment = VerticalAlignment.Top;
            NeuronViewGrid.Children.Add(r);
            rectangles.Add(r);
            return r;
        }
        Line getLine() {
            Line l = new Line();
            l.Stroke = blueBrush;
            l.StrokeThickness = 1;
            l.HorizontalAlignment = HorizontalAlignment.Left;
            l.VerticalAlignment = VerticalAlignment.Top;
            NeuronViewGrid.Children.Add(l);
            lines.Add(l);
            return l;
        }

        void Redraw() {
            statusLabel.Content = string.Format(
                "Generation: {0}  Species: {1}  Genome: {2}  Max Fitness: {3} Current Fitness: {4}  Current Frame: {5}",
                NetworkController.pool.generation,
                NetworkController.pool.currentSpecies,
                NetworkController.pool.currentGenome,
                NetworkController.pool.maxFitness,
                NetworkController.pool.currentFitness,
                NetworkController.pool.currentFrame);

            #region game grid
            if (tabControl.SelectedIndex == 0) {
                double bw = gameGrid.Width / NetworkController.game.gameSize;
                double bh = gameGrid.Height / NetworkController.game.gameSize;
                for (int x = 0; x < NetworkController.game.gameSize; x++)
                    for (int y = 0; y < NetworkController.game.gameSize; y++) {
                        gameLabelGrid[x, y].Width = bw;
                        gameLabelGrid[x, y].Height = bh;

                        if (NetworkController.game.tiles[x, y].isExposed) {
                            gameLabelGrid[x, y].Background = NetworkController.game.tiles[x, y].isMine ? redBrush : whiteBrush;
                            gameLabelGrid[x, y].Content =
                                NetworkController.game.tiles[x, y].isMine ? "X" :
                                (NetworkController.game.tiles[x, y].neighborMineCount > 0 ? NetworkController.game.tiles[x, y].neighborMineCount.ToString() : "");
                        } else {
                            gameLabelGrid[x, y].Background = NetworkController.game.tiles[x, y].isMine ? lightRedBrush : grayBrush;
                            gameLabelGrid[x, y].Content = "";
                        }

                        if (NetworkController.outputs != null && NetworkController.curMoves.Any(p => p.x == x && p.y == y))
                            gameLabelGrid[x, y].Background = yellowBrush;
                    }
            }
            #endregion
            #region neuron i/o grid
            if (tabControl.SelectedIndex == 2 && NetworkController.pool != null && NetworkController.inputs != null && NetworkController.outputs != null) {
                #region grids
                double iow = inputGrid.Width / NetworkController.game.gameSize;
                double ioh = inputGrid.Height / NetworkController.game.gameSize;
                for (int x = 0; x < NetworkController.game.gameSize; x++)
                    for (int y = 0; y < NetworkController.game.gameSize; y++) {
                        outputLabelGrid[x, y].Width = inputLabelGrid[x, y].Width = iow;
                        outputLabelGrid[x, y].Height = inputLabelGrid[x, y].Height = ioh;
                        
                            float input = NetworkController.inputs[x + y * NetworkController.game.gameSize];
                            float output = NetworkController.outputs[x + y * NetworkController.game.gameSize];

                            byte i = (byte)(255 * ((input + 1) / 3f));
                            byte o = (byte)(255 * ((input + 1) * .5f));

                            inputLabelGrid[x, y].Background = new SolidColorBrush(Color.FromRgb(i, i, i));
                            outputLabelGrid[x, y].Background = new SolidColorBrush(Color.FromRgb(o, o, o));
                    }
                #endregion

                foreach (Rectangle rect in rectangles)
                    NeuronViewGrid.Children.Remove(rect);
                foreach (Line line in lines)
                    NeuronViewGrid.Children.Remove(line);
                rectangles.Clear();
                lines.Clear();

                Species species = NetworkController.pool.species[NetworkController.pool.currentSpecies];
                Genome genome = species.genomes[NetworkController.pool.currentGenome];
                Network network = genome.network;

                Dictionary<int, Vector> neuronPositions = new Dictionary<int, Vector>();
                Dictionary<int, Rectangle> neuronRectangles = new Dictionary<int, Rectangle>();

                System.Windows.Point min = NeuronArea.TransformToAncestor(NeuronViewGrid).Transform(new System.Windows.Point(0, 0));
                System.Windows.Point max = NeuronArea.TransformToAncestor(NeuronViewGrid).Transform(new System.Windows.Point(NeuronArea.ActualWidth, NeuronArea.ActualHeight));
                System.Windows.Point mid = new System.Windows.Point((min.X + max.X) * .5, (min.Y + max.Y) * .5);

                System.Windows.Point inmin = inputGrid.TransformToAncestor(NeuronViewGrid).Transform(new System.Windows.Point(0, 0));
                System.Windows.Point outmin = outputGrid.TransformToAncestor(NeuronViewGrid).Transform(new System.Windows.Point(0, 0));

                #region middle neurons
                foreach (KeyValuePair<int, Neuron> kp in network.neurons) {
                    if (kp.Key >= NetworkController.InputCount && kp.Key < NetworkController.MaxNodes) {
                        // Make rectangle for middle neurons
                        Rectangle rect = getRect();
                        rect.Margin = new Thickness(Width * .5 - rect.Width * .5, inputGrid.Margin.Top + inputGrid.Height * .5 - rect.Height * .5, 0, 0);
                        neuronPositions[kp.Key] = new Vector(Width * .5, inputGrid.Margin.Top + inputGrid.Height * .5);
                        neuronRectangles[kp.Key] = rect;

                    } else if (kp.Key < NetworkController.InputCount) {
                        if (kp.Key == NetworkController.InputCount - 1) // don't make a rectangle for the bias cell (its already there)
                            neuronPositions[kp.Key] = new Vector(biasCell.Margin.Left + biasCell.Width * .5, biasCell.Margin.Top + biasCell.Height * .5);
                        else
                            neuronPositions[kp.Key] = new Vector(
                                iow * .5 + inmin.X +
                                    (kp.Key % NetworkController.game.gameSize) * iow,
                                
                                ioh * .5 + inmin.Y +
                                    (kp.Key / NetworkController.game.gameSize) * ioh
                            );


                    } else if (kp.Key >= NetworkController.MaxNodes) {
                        neuronPositions[kp.Key] = new Vector(
                            iow * .5 + outmin.X +
                                ((kp.Key - NetworkController.MaxNodes) % NetworkController.game.gameSize) * iow,

                            ioh * .5 + outmin.Y +
                                ((kp.Key - NetworkController.MaxNodes) / NetworkController.game.gameSize) * ioh
                        );
                    }
                }
                #endregion
                
                #region gene lines
                int gc = 0;
                foreach (Gene gene in genome.genes) {
                    if (gene.enabled) {
                        gc++;
                        Vector a = neuronPositions[gene.into], b = neuronPositions[gene.@out];

                        if (gene.into >= NetworkController.InputCount && gene.into < NetworkController.MaxNodes) {
                            // going into an intermediate neuron; reposition the neuron's box
                            a.Y = (a.Y + b.Y) * .5;
                            a.X = (a.X + Math.Max(b.X, mid.X)) * .5;

                            if (a.X < min.X + 20) a.X = min.X + 20;
                            if (a.X > max.X - 20) a.X = max.X - 20;
                        
                            neuronRectangles[gene.into].Margin = new Thickness(
                                a.X - neuronRectangles[gene.into].Width * .5,
                                a.Y - neuronRectangles[gene.into].Height * .5, 0, 0);
                        }
                        else if (gene.@out >= NetworkController.InputCount && gene.@out < NetworkController.MaxNodes) {
                            // going into an intermediate neuron; reposition the neuron's box
                            b.X = (Math.Min(a.X, mid.X) + b.X) * .5;
                            b.Y = (a.Y + b.Y) * .5;
                            if (b.X < min.X + 20) b.X = min.X + 20;
                            if (b.X > max.X - 20) b.X = max.X - 20;
                        
                            neuronRectangles[gene.@out].Margin = new Thickness(
                                b.X - neuronRectangles[gene.@out].Width * .5,
                                b.Y - neuronRectangles[gene.@out].Height * .5, 0, 0);
                        }

                        Line line = getLine();
                        line.Margin = new Thickness(0);
                        line.X1 = a.X;
                        line.Y1 = a.Y;
                        line.X2 = b.X;
                        line.Y2 = b.Y;
                    }
                }
                #endregion

                NeuronViewLabel.Content = genome.network.neurons.Count + " neurons, " + gc + " enabled genes";
            }
            #endregion
        }
        
        void EnableDisableButton_Click(object sender, RoutedEventArgs e) {
            if (timer.IsEnabled) {
                timer.Stop();
                EnableDisableButton.Content = "Start";
            } else {
                timer.Start();
                EnableDisableButton.Content = "Stop";
            }
        }
        void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e) {
            e.Handled = new Regex("[^0-9.-]+").IsMatch(e.Text);
            try {
                if (Convert.ToInt32(updateFreq.Text) == 0)
                    e.Handled = true;
            } catch { e.Handled = true; }

            timer.Interval = TimeSpan.FromSeconds(1 / Convert.ToDouble(updateFreq.Text));
            if (timer.IsEnabled) {
                timer.Stop();
                timer.Start();
            }
        }
        
        void Window_Loaded(object sender, RoutedEventArgs e) {
            NetworkController.Initialize(new Game(16, 35, 5));

            SetupGrid();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1 / 90d);
            timer.Tick += Timer_Tick;

            Closing += (object s, System.ComponentModel.CancelEventArgs ea) => { timer.Stop(); };
            SizeChanged += (object s, SizeChangedEventArgs ea) => { Redraw(); };
            KeyDown += (object s, KeyEventArgs ea) => { if (ea.Key == Key.Space && !timer.IsEnabled) Timer_Tick(null, null); };
            updateFreq.KeyDown += (object s, KeyEventArgs ea) => { if (ea.Key == Key.Enter) Keyboard.ClearFocus(); };

            tabControl.SelectionChanged += (object s, SelectionChangedEventArgs g) => { Redraw(); };

            SaveButton.Click += (object s, RoutedEventArgs ea) => { NetworkController.Save(SaveName.Text); };
            LoadButton.Click += (object s, RoutedEventArgs ea) => { timer.Stop();  NetworkController.Load(SaveName.Text); };
        }

        void PlayTopButton_Click(object sender, RoutedEventArgs e) {
            timer.Stop();
            NetworkController.LoadTop();
            Redraw();
        }

        private void noDelay_Checked(object sender, RoutedEventArgs e) {
            updateFreq.IsEnabled = false;
            timer.Interval = TimeSpan.FromSeconds(0);
        }
        private void noDelay_Unchecked(object sender, RoutedEventArgs e) {
            updateFreq.IsEnabled = true;
            timer.Interval = TimeSpan.FromSeconds(1 / Convert.ToDouble(updateFreq.Text));
        }
    }
}
