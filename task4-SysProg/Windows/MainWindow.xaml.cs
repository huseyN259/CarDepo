using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Windows;
using task4_SysProg.Models;

namespace task4_SysProg;

public partial class MainWindow : Window
{
	DirectoryInfo? directoryInfo = null;
	Stopwatch? stopwatch = null;
	public ObservableCollection<Car>? Cars { get; set; }
	CancellationTokenSource? cts = new();

	public MainWindow()
	{
		InitializeComponent();

		DataContext = this;
		Cars = new();
	}

	private void SingleMode(CancellationToken token)
	{
		Cars?.Clear();

		new Thread(() =>
		{
			stopwatch = Stopwatch.StartNew();
			directoryInfo = new DirectoryInfo(@"..\..\..\CarsData");

			foreach (var file in directoryInfo.GetFiles())
			{
				var jsonTxt = File.ReadAllText(file.FullName);
				var carlist = JsonSerializer.Deserialize<List<Car>>(jsonTxt);
				foreach (var car in carlist)
				{
					if (token.IsCancellationRequested)
					{
						stopwatch.Stop();

						Dispatcher.Invoke(() => timeSpan.Text = stopwatch.Elapsed.ToString());
						Dispatcher.Invoke(() => BtnStart.IsEnabled = true);
						Dispatcher.Invoke(() => BtnCancel.IsEnabled = false);

						break;
					}

					Dispatcher.Invoke(() => Cars?.Add(car));
					Dispatcher.Invoke(() => timeSpan.Text = stopwatch.Elapsed.ToString());

					Thread.Sleep(300);
				}
			}

			stopwatch.Stop();

			Dispatcher.Invoke(() => timeSpan.Text = stopwatch.Elapsed.ToString());
			Dispatcher.Invoke(() => BtnStart.IsEnabled = true);
			Dispatcher.Invoke(() => BtnCancel.IsEnabled = false);
		}).Start();
	}

	private void MultiMode(CancellationToken token)
	{
		Cars?.Clear();
		directoryInfo = new DirectoryInfo(@"..\..\..\CarsData");
		stopwatch = Stopwatch.StartNew();

		foreach (var file in directoryInfo.GetFiles())
		{
			ThreadPool.QueueUserWorkItem(state =>
			{
				var jsonTxt = File.ReadAllText(file.FullName);
				var carlist = JsonSerializer.Deserialize<List<Car>>(jsonTxt);
				foreach (var car in carlist)
				{
					if (token.IsCancellationRequested)
					{
						stopwatch.Stop();

						Dispatcher.Invoke(() => timeSpan.Text = stopwatch.Elapsed.ToString());
						Dispatcher.Invoke(() => BtnStart.IsEnabled = true);
						Dispatcher.Invoke(() => BtnCancel.IsEnabled = false);

						break;
					}

					Dispatcher.Invoke(() => Cars?.Add(car));
					Dispatcher.Invoke(() => timeSpan.Text = stopwatch.Elapsed.ToString());
					
					Thread.Sleep(300);
				}

				Dispatcher.Invoke(() => BtnStart.IsEnabled = true);
				Dispatcher.Invoke(() => BtnCancel.IsEnabled = false);
			});
		}
	}

	private void BtnStart_Click(object sender, RoutedEventArgs e)
	{
		if (singleMode.IsChecked == false && multiMode.IsChecked == false)
		{
			MessageBox.Show("You must choose any Mode...", "Car Depo", MessageBoxButton.OK, MessageBoxImage.Warning);
			return;
		}

		BtnStart.IsEnabled = false;
		BtnCancel.IsEnabled = true;

		cts = new();

		if (singleMode.IsChecked == true) SingleMode(cts.Token);
		if (multiMode.IsChecked == true) MultiMode(cts.Token);
	}

	private void BtnCancel_Click(object sender, RoutedEventArgs e)
	{
		cts?.Cancel();

		BtnCancel.IsEnabled = false;
		BtnStart.IsEnabled = true;

		timeSpan.Text = "00:00:00";
	}
}