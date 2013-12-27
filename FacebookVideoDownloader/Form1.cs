using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using FacebookVideoDownloader.Properties;

namespace FacebookVideoDownloader
{
	public sealed partial class Form1 : Form
	{
		private bool _facebookok;
		private bool _fileok;
		private readonly Regex _rx = new Regex("video_data\":\\[({.*?})\\]");

		public Form1()
		{
			InitializeComponent();
			Start.Enabled = false;
			saveFileDialog1.DefaultExt = ".bin";
			saveFileDialog1.AddExtension = true;
			saveFileDialog1.Filter = Resources.Filter;
		}

		private void button2_Click(object sender, EventArgs e)
		{
			saveFileDialog1.ShowDialog();
			if (_fileok = !string.IsNullOrEmpty(saveFileDialog1.FileName))
				label2.Text = saveFileDialog1.FileName;

			CheckOk();
		}

		private void CheckOk()
		{
			Start.Enabled = _facebookok && _fileok;
		}

		private void button1_Click(object sender, EventArgs e) // start
		{
			webBrowser1.Navigate(new Uri(textBox1.Text));
			label2.Text = Resources.Loading;
		}

		private void textBox1_TextChanged(object sender, EventArgs e)
		{
			_facebookok = Regex.IsMatch(textBox1.Text, @"^https?://(:?www\.)?facebook\.com.*$");
			CheckOk();
		}

		private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
		{
			label2.Text = Resources.WaitingForContent;
			int idx;
			while ((idx = webBrowser1.DocumentText.IndexOf("flashvars", StringComparison.Ordinal)) == -1)
				webBrowser1.Refresh(WebBrowserRefreshOption.Normal);

			var contents = HttpUtility.UrlDecode(webBrowser1.DocumentText.Substring(idx));

			label2.Text = Resources.MatchRegEx;
			var mg = _rx.Match(contents ?? "");
			if (!String.IsNullOrEmpty(mg.Groups[1].ToString()))
			{
				label2.Text = Resources.ParsingJSON;
				var jss = new JavaScriptSerializer();
				var obj = jss.Deserialize<Urls>(mg.Groups[1].ToString());
				string videourl = !String.IsNullOrEmpty(obj.hd_src) ? obj.hd_src : obj.sd_src;
				label2.Text = Resources.DownloadingFile;
				videourl = videourl.Replace(@"\", "");
				saveFileDialog1.FileName = saveFileDialog1.FileName.Remove(saveFileDialog1.FileName.Length - 4) + Path.GetExtension(videourl).Remove(Path.GetExtension(videourl).IndexOf('?')); //fix extension.. roughly
				using (var wr2 = new StreamWriter(saveFileDialog1.FileName).BaseStream)
				{
					var responseStream = WebRequest.Create(videourl).GetResponse().GetResponseStream();
					if (responseStream != null)
						responseStream.CopyTo(wr2);
				}

				label2.Text = Resources.Done;
			}
			else
			{
				label2.Text = Resources.NoMatch;
			}
		}
	}

	internal class Urls
	{
		public string sd_src { get; set; }
		public string hd_src { get; set; }
	}
}
