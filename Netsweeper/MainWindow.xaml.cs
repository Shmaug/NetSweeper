using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NetSweeper {
    public partial class MainWindow : Window {
        SolidColorBrush redBrush = new SolidColorBrush(Color.FromRgb(255, 0, 0));
        SolidColorBrush blackBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        SolidColorBrush grayBrush = new SolidColorBrush(Color.FromRgb(127, 127, 127));
        SolidColorBrush whiteBrush = new SolidColorBrush(Color.FromRgb(255, 255, 255));

        Label[,] labelGrid;
        DispatcherTimer timer;

        public MainWindow() {
            InitializeComponent();
        }

        private void Timer_Tick(object sender, EventArgs e) {
            NetworkController.Update();
            Redraw();
        }

        void SetupGrid() {
            gameGrid.Columns = NetworkController.game.gameSize;
            gameGrid.Rows = NetworkController.game.gameSize;
            labelGrid = new Label[NetworkController.game.gameSize, NetworkController.game.gameSize];
            double bw = gameGrid.Width / NetworkController.game.gameSize;
            double bh = gameGrid.Height / NetworkController.game.gameSize;
            for (int x = 0; x < NetworkController.game.gameSize; x++)
                for (int y = 0; y < NetworkController.game.gameSize; y++) {
                    labelGrid[x, y] = new Label();
                    labelGrid[x, y].Width = bw;
                    labelGrid[x, y].Height = bh;
                    //labelGrid[x, y].MouseDown += Label_MouseDown;
                    labelGrid[x, y].BorderBrush = blackBrush;
                    labelGrid[x, y].BorderThickness = new Thickness(1, 1, 1, 1);
                    labelGrid[x, y].Background = grayBrush;
                    labelGrid[x, y].HorizontalContentAlignment = HorizontalAlignment.Center;
                    labelGrid[x, y].VerticalContentAlignment = VerticalAlignment.Center;
                    labelGrid[x, y].FontSize = 14;
                    labelGrid[x, y].Tag = new Point(x, y);

                    gameGrid.Children.Add(labelGrid[x, y]);
                }
        }
        void Redraw() {
            double bw = gameGrid.Width / NetworkController.game.gameSize;
            double bh = gameGrid.Height / NetworkController.game.gameSize;
            for (int x = 0; x < NetworkController.game.gameSize; x++)
                for (int y = 0; y < NetworkController.game.gameSize; y++) {
                    if (NetworkController.game.tiles[x, y].isExposed) {
                        labelGrid[x, y].Width = bw;
                        labelGrid[x, y].Height = bh;
                        labelGrid[x, y].Background = NetworkController.game.tiles[x, y].isMine ? redBrush : whiteBrush;
                        labelGrid[x, y].Content =
                            NetworkController.game.tiles[x, y].isMine ? "X" :
                            (NetworkController.game.tiles[x, y].neighborMineCount > 0 ? NetworkController.game.tiles[x, y].neighborMineCount.ToString() : "");
                    } else {
                        labelGrid[x, y].Background = grayBrush;
                        labelGrid[x, y].Content = "";
                    }
                }
            statusLabel.Content = string.Format(
                "Generation: {0}  Species: {1}  Genome: {2}  Max Fitness: {3}  Move: {4}",
                NetworkController.pool.generation,
                NetworkController.pool.currentSpecies,
                NetworkController.pool.currentGenome,
                NetworkController.pool.maxFitness,
                NetworkController.pool.currentMove);
        }

        //private void Label_MouseDown(object sender, MouseButtonEventArgs e) {
        //    if (sender is Label) {
        //        Label l = sender as Label;
        //        if (l.Tag is Point) {
        //            NetworkController.game.Move((l.Tag as Point?).Value.x, (l.Tag as Point?).Value.y);
        //            Redraw();
        //        }
        //    }
        //}

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            timer.Stop();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Space && manualStepCheckBox.IsChecked.Value)
                Timer_Tick(null, null);
        }
        private void manualStepCheckBox_Checked(object sender, RoutedEventArgs e) {
            timer.Stop();
        }
        private void manualStepCheckBox_Unchecked(object sender, RoutedEventArgs e) {
            timer.Start();
        }

        private void EnableDisableButton_Click(object sender, RoutedEventArgs e) {
            if (timer.IsEnabled) {
                timer.Stop();
                EnableDisableButton.Content = "Start";
            } else {
                timer.Start();
                EnableDisableButton.Content = "Stop";
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            NetworkController.Initialize(new Game(16, 35, 5));

            SetupGrid();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(.25);
            timer.Tick += Timer_Tick;
        }
        
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e) {
            e.Handled = new Regex("[^0-9.-]+").IsMatch(e.Text);
            if (Convert.ToInt32(updateFreq.Text) == 0)
                e.Handled = true;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e) {
            timer.Interval = TimeSpan.FromSeconds(1 / Convert.ToDouble(updateFreq.Text));
        }
    }
}
