using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Net;
using System.Xml;
using System.Threading;
using System.Windows.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Drawing;

namespace octodexGui
{
	public partial class MainWindow : Window
	{
		public Random rnd;
		public MainWindow()
		{
			InitializeComponent();
			rnd = new Random();

			//thread
			try
			{
				Thread thread = new Thread(new ThreadStart(Loader.LoadXML));
				thread.Start();
			}
			catch (Exception)
			{
				MessageBox.Show("Something went wrong");
			}

			//timer
			DispatcherTimer dispatcherTimer = new DispatcherTimer();
			dispatcherTimer.Tick += dispatcherTimer_Tick;
			dispatcherTimer.Interval = new TimeSpan(0, 0, 10);
			dispatcherTimer.Start();
		}

		protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
		{
			if ((e.Key == Key.Escape) || (e.Key == Key.F11))
			{
				#region Fullscreen
				if (_fullScreen)
				{
					this.WindowState = _previousWindowState;
					this.WindowStyle = WindowStyle.SingleBorderWindow;

					_fullScreen = false;
					e.Handled = true;
				}
				else if (e.Key == Key.F11)
				{

					_previousWindowState = this.WindowState;
					this.WindowStyle = WindowStyle.None;
					this.WindowState = WindowState.Maximized;

					_fullScreen = true;
					e.Handled = true;
				}
				#endregion
			}
			UpdateScale();
		}

		private bool _fullScreen = false;
		private WindowState _previousWindowState;

		private void dispatcherTimer_Tick(object sender, EventArgs e)
		{
			if (Status.Bild)
			{
				octodexElement rndElement = GetRndBild();
				bild.Source = new BitmapImage(new Uri(rndElement.DlName));
				titel.Content = rndElement.Name;
			}
		}

		private octodexElement GetRndBild()
		{
			var col = Loader.collection.Where(o => o.DlName != null).ToArray();
			return col[rnd.Next(0, col.Count())];
		}

		private void Grid_LayoutUpdated_1(object sender, EventArgs e)
		{
			UpdateScale();
		}

		private void UpdateScale()
		{
			double faktor = ((Panel)Application.Current.MainWindow.Content).ActualHeight > ((Panel)Application.Current.MainWindow.Content).ActualWidth ? ((Panel)Application.Current.MainWindow.Content).ActualWidth : ((Panel)Application.Current.MainWindow.Content).ActualHeight;
			titel.FontSize = faktor * 0.08;
			bild.Margin = new Thickness(10, (50 + faktor * 0.08), 10, 10);
		}
	}

	public class octodexElement
	{
		public string Name { get; private set; }
		public string Bildlink { get; private set; }
		public string DlName { get; set; }
		public octodexElement(string name, string bildLink)
		{
			Name = 
			Name += name;
			Bildlink = bildLink;
			DlName = null;
		}
	}

	public static class Status
	{
		public static bool Bild;
	}

	public static class Loader
	{
		public static List<octodexElement> collection;
		private static string _baseUrl = "http://octodex.github.com/images/";
		public static void LoadXML()
		{
			collection = new List<octodexElement>();

			XmlDocument doc = new XmlDocument();
			doc.LoadXml(new WebClient().DownloadString("http://feeds.feedburner.com/Octocats.xml"));
			XmlElement root = doc.DocumentElement;

			foreach (XmlNode node in root.ChildNodes)
			{
				if (node.Name == "entry")
				{
					collection.Add(new octodexElement(node["title"].InnerText, Regex.Match(node["content"].InnerXml, "<img.+?src=[\"'](.+?)[\"'].+?>", RegexOptions.IgnoreCase).Groups[1].Value));
				}
			}

			foreach (octodexElement bild in collection)
			{
				string _uri = System.IO.Path.Combine(System.IO.Path.GetTempPath(),"octodex", bild.Bildlink.Replace(_baseUrl, ""));
				Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "octodex"));
				if (!File.Exists(_uri) || new FileInfo(_uri).Length == 0 || !IsValidImage(_uri))
				{
					if (!DLFile(bild))
					{
						System.Threading.Thread.Sleep(50);
						DLFile(bild);
					}
				}
				if (File.Exists(_uri))
				{
					bild.DlName = _uri;
					Status.Bild = true;
				}
			}
		}

		private static bool IsValidImage(string filename)
		{
			try
			{
				using (var bmp = new Bitmap(filename)){}
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private static bool DLFile(octodexElement bild)
		{
			try
			{
				new WebClient().DownloadFile(bild.Bildlink, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "octodex", bild.Bildlink.Replace(_baseUrl, "")));
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
}