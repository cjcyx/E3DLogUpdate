using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace E3DLogUpdate
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }
        public static string servername = "";
        public static string path = @"C:\AVEVA\AVEVA Licensing System\RMS\";
        protected override void OnStart(string[] args)
        {
            //OnLicChange();
            //var task = Task.Factory.StartNew(() =>
           // {
                OnfileChange();
           // });
            //var DelayFileSystemWatcher1 = new DelayFileSystemWatcher(@"E:\AVEVA\AVEVA Licensing System\RMS", "lservrc_AVEVA", Lic_Changed, 1500);
            var DelayFileSystemWatcher = new DelayFileSystemWatcher(path, "usagelog.log", fileSystemWatcher_Changed, 1500);
            FileStream fs = new FileStream(@"e:\log.txt", FileMode.OpenOrCreate, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);
            servername = Environment.MachineName;
            sw.BaseStream.Seek(0, SeekOrigin.End);
            sw.WriteLine("WindowsService: Service Started" + DateTime.Now.ToString() + "\n");
            sw.WriteLine(servername);
            sw.Flush();
            sw.Close();
            fs.Close();
        }

        protected override void OnStop()
        {
            FileStream fs = new FileStream(@"e:\log.txt", FileMode.OpenOrCreate, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);
            sw.BaseStream.Seek(0, SeekOrigin.End);
            sw.WriteLine("WindowsService: Service Stopped" + DateTime.Now.ToString() + "\n");
            sw.Flush();
            sw.Close();
            fs.Close();
        }
        
        private static List<string> fileContains(string path)
        {
            LineData lineData = new LineData() { data = "Get", lineNo = 0, serverName = Environment.MachineName };
            string jsDate = JsonConvert.SerializeObject(lineData);
            var linesCount = PostWebRequest("http://172.16.5.36:8082/ReceivedInfos/PostLine", jsDate);
            var lastrows = Convert.ToInt32(linesCount);
            List<string> str = new List<string>();
            using (StreamReader sr = new StreamReader(path, System.Text.Encoding.UTF8))
            {
                int i = 0;
                while (sr.Peek() > -1)
                {
                    i++;
                    var line = sr.ReadLine();
                    if (i > lastrows)
                    {
                        str.Add(line);
                        lastrows++;
                    }
                }
            }
            if(lastrows< Convert.ToInt32(linesCount))
            {
                for(int j = 0; j < 100; j++)
                {
                    if (!File.Exists(path + "." +j.ToString("D2")))
                    {
                        j = j - 1;
                        str = new List<string>();
                        using (StreamReader sr = new StreamReader(path + "." + j.ToString("D2"), System.Text.Encoding.UTF8))
                        {
                            int i = 0;
                            while (sr.Peek() > -1)
                            {
                                i++;
                                var line = sr.ReadLine();
                                if (i > lastrows)
                                {
                                    str.Add(line);
                                    lastrows++;
                                }
                            }
                        }
                    }
                }
                lastrows =0;
                using (StreamReader sr = new StreamReader(path, System.Text.Encoding.UTF8))
                {
                    int i = 0;
                    while (sr.Peek() > -1)
                    {
                        i++;
                        var line = sr.ReadLine();
                        if (i > lastrows)
                        {
                            str.Add(line);
                            lastrows++;
                        }
                    }
                }
            }
            lineData = new LineData() { data = "Post", lineNo = lastrows, serverName = Environment.MachineName };
            jsDate = JsonConvert.SerializeObject(lineData);
            linesCount = PostWebRequest("http://172.16.5.36:8082/ReceivedInfos/PostLine", jsDate);
            return str;
        }
        private static Dictionary<string, string> GetName()
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            XmlDocument doc = new XmlDocument();
            doc.Load(path+"usagelog.xml");
            XmlNode xn = doc.SelectSingleNode("map");
            XmlNodeList xnl = xn.ChildNodes;
            foreach (XmlNode xn1 in xnl)
            {
                XmlElement xe = (XmlElement)xn1;
                string anonymous = xe.GetAttribute("Anonymous").ToString();
                string original = xe.GetAttribute("Original").ToString();
                if (anonymous != "")
                {
                    dic.Add(anonymous, original);
                }
            }
            return dic;
        }
        private static void fileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    //var data = "data=Post&lineNo=0&serverName=" + Environment.MachineName;
                    //var linesCount = PostWebRequest("https://172.16.5.130:8081/PostGetLines.php", data, Encoding.UTF8);
                    break;
                case WatcherChangeTypes.Deleted:
                    //TODO                        
                    break;
                case WatcherChangeTypes.Changed:
                    OnfileChange();
                    break;
                default:
                    break;
            }
        }
        private static void Lic_Changed(object sender, FileSystemEventArgs e)
        {
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    //var data = "data=Post&lineNo=0&serverName=" + Environment.MachineName;
                    //var linesCount = PostWebRequest("https://172.16.5.130:8081/PostGetLines.php", data, Encoding.UTF8);
                    OnLicChange();
                    break;
                case WatcherChangeTypes.Deleted:
                    //TODO                        
                    break;
                case WatcherChangeTypes.Changed:
                    OnLicChange();
                    break;
                default:
                    break;
            }
        }
        private static void OnLicChange()
        {
            LineData licData = new LineData() { data = "Get", lineNo =0, serverName = Environment.MachineName };
            string jsDate = JsonConvert.SerializeObject(licData);
            var module = PostWebRequest("http://172.16.5.36:8082/ReceivedInfos/GetLicData", jsDate);
            int count = 0;
            using (StreamReader sr = new StreamReader(@"C:\AVEVA\AVEVA Licensing System\RMS\lservrc_AVEVA", System.Text.Encoding.UTF8))
            {
                while (sr.Peek() > -1)
                {
                    var line = sr.ReadLine();
                    try
                    {
                        if(line.Contains(module))
                        {
                            var tmp = line.Split(' ');
                            if (tmp[1] == module)
                            {
                                count += Convert.ToInt32(tmp[7].Replace("_KEYS", ""));
                            }
                        }
                    }
                    catch
                    {

                    }
                }
            }
            licData = new LineData() { data = "Post", lineNo = count, serverName = Environment.MachineName };
            jsDate = JsonConvert.SerializeObject(licData);
            module = PostWebRequest("http://172.16.5.36:8082/ReceivedInfos/GetLicData", jsDate);

        }
        private static void OnfileChange()
        {
            var dic = GetName();
            var items = fileContains(path+"usagelog.log");
            for (int i = 0; i < items.Count; i += 50)
            {
                List<ReceivedInfos> test = new List<ReceivedInfos>();
                for (int j = i; j < i + 50 && j < items.Count; j++)
                {
                    if (items[j].Substring(0, 1) != "#")
                    {
                        var str = items[j].Split(' ');
                        test.Add(new ReceivedInfos()
                        {
                            timeStamp = Convert.ToInt32(str[9]),
                            module = str[10],
                            status = Convert.ToInt32(str[12]),
                            currentUsage = Convert.ToInt32(str[13]),
                            compName = dic.ContainsKey(str[16]) ? dic[str[16]] : str[16],
                            userName = dic.ContainsKey(str[15]) ? dic[str[15]] : str[15],
                            serverName = Environment.MachineName
                        });
                    }
                }
                string jsDate = JsonConvert.SerializeObject(test);
                var modlueStr = PostWebRequest("http://172.16.5.36:8082/ReceivedInfos/POSTLOG", jsDate);
            }
        }
        private static string PostWebRequest(string postUrl, string paramData)
        {
            var request = (HttpWebRequest)WebRequest.Create(postUrl);
            request.Method = "POST";
            request.ContentType = "application/json;charset=UTF-8";
            byte[] byteData = Encoding.UTF8.GetBytes(paramData);
            int length = byteData.Length;
            request.ContentLength = length;
            Stream writer = request.GetRequestStream();
            writer.Write(byteData, 0, length);
            writer.Close();
            var response = (HttpWebResponse)request.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding("utf-8")).ReadToEnd();
            return responseString;
        }
    }
    public class Infos
    {
        public int timeStamp;
        public string module;
        public int status;
        public int currentUsage;
        public string compName;
        public string userName;
        public Infos(int timeStamp, string module, int status, int currentUsage, string compName, string userName)
        {
            this.timeStamp = timeStamp;
            this.module = module;
            this.status = status;
            this.currentUsage = currentUsage;
            this.compName = compName;
            this.userName = userName;
        }
    }
    public class DelayFileSystemWatcher
    {
        private readonly Timer m_Timer;
        private readonly Int32 m_TimerInterval;
        private readonly FileSystemWatcher m_FileSystemWatcher;
        private readonly FileSystemEventHandler m_FileSystemEventHandler;
        private readonly Dictionary<String, FileSystemEventArgs> m_ChangedFiles = new Dictionary<string, FileSystemEventArgs>();

        public DelayFileSystemWatcher(string path, string filter, FileSystemEventHandler watchHandler, int timerInterval)
        {
            m_Timer = new Timer(OnTimer, null, Timeout.Infinite, Timeout.Infinite);
            m_FileSystemWatcher = new FileSystemWatcher(path, filter);
            m_FileSystemWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime;
            m_FileSystemWatcher.Created += fileSystemWatcher_Changed;
            m_FileSystemWatcher.Changed += fileSystemWatcher_Changed;
            m_FileSystemWatcher.Deleted += fileSystemWatcher_Changed;
            m_FileSystemWatcher.Renamed += fileSystemWatcher_Changed;
            m_FileSystemWatcher.EnableRaisingEvents = true;
            m_FileSystemEventHandler = watchHandler;
            m_TimerInterval = timerInterval;
        }

        public void fileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            lock (m_ChangedFiles)
            {
                if (!m_ChangedFiles.ContainsKey(e.Name))
                {
                    m_ChangedFiles.Add(e.Name, e);
                }
            }
            m_Timer.Change(m_TimerInterval, Timeout.Infinite);
        }

        private void OnTimer(object state)
        {
            Dictionary<String, FileSystemEventArgs> tempChangedFiles = new Dictionary<String, FileSystemEventArgs>();

            lock (m_ChangedFiles)
            {
                foreach (KeyValuePair<string, FileSystemEventArgs> changedFile in m_ChangedFiles)
                {
                    tempChangedFiles.Add(changedFile.Key, changedFile.Value);
                }
                m_ChangedFiles.Clear();
            }

            foreach (KeyValuePair<string, FileSystemEventArgs> changedFile in tempChangedFiles)
            {
                m_FileSystemEventHandler(this, changedFile.Value);
            }
        }
    }
}
