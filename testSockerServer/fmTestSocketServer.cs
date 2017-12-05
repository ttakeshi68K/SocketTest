using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
// C#.NETのソケット通信サンプル
using System.Threading;
using System.Net;
using System.Net.Sockets;

using log4net;

using System.Diagnostics;

using System.Configuration;

using testSockerServer.Properties;


namespace testSockerServer
{
    public partial class fmTestSocketServer : Form
    {
        public fmTestSocketServer()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ログ出力機能
        /// </summary>
        private static ILog _iLog = LogManager.GetLogger("InternalLog");

        // ソケット・リスナー
        private TcpListener myListener;
        // クライアント送受信
        private ClientTcpIp[] myClient = new ClientTcpIp[4];

        private static string rcvMsg = string.Empty;

        // フォームロード時のソケット接続処理

        private void fmTestSocketServer_Load(object sender, EventArgs e)
        {

            bool l4nSetingLoadSucess = _iLog.Logger.Repository.Configured;

            if (!l4nSetingLoadSucess)
            {
                var errorIventMsg = "log4net.xml load Error:Repository.Configured=" + l4nSetingLoadSucess.ToString();

                EventLog.WriteEntry("testSocketServer", errorIventMsg, EventLogEntryType.Error, 9999);

                throw new Exception();
            }

            // IPアドレス＆ポート番号設定
            // int myPort = 30000;

            int myPort = Settings.Default.socketPort;
            
            //IPAddress myIp = Dns.Resolve("localhost").AddressList[0]; // 旧バージョン
            IPAddress myIp = Dns.GetHostEntry("localhost").AddressList[0];
            IPEndPoint myEndPoint = new IPEndPoint(myIp, myPort);

            // リスナー開始
            myListener = new TcpListener(myEndPoint);
            myListener.Start();

            // クライアント接続待ち開始
            Thread myServerThread = new Thread(new ThreadStart(ServerThread));
            myServerThread.Start();
        }

        // フォームクローズ時のソケット切断処理
        private void fmTestSocketServer_FormClosed(object sender, FormClosedEventArgs e)
        {
            // リスナー終了
            myListener.Stop();
            // クライアント切断
            for (int i = 0; i <= myClient.GetLength(0) - 1; i++)
            {
                if (myClient[i] != null)
                {
                    if (myClient[i].objSck.Connected == true)
                    {
                        // ソケットクローズ
                        myClient[i].objStm.Close();
                        myClient[i].objSck.Close();
                    }
                }
            }
        }

        // クライアント接続待ちスレッド
        private void ServerThread()
        {
            try
            {
                int intNo;
                while (true)
                {
                    // ソケット接続待ち
                    TcpClient myTcpClient = myListener.AcceptTcpClient();
                    // クライアントから接続有り
                    for (intNo = 0; intNo <= myClient.GetLength(0) - 1; intNo++)
                    {
                        if (myClient[intNo] == null)
                        {
                            break;
                        }
                        else if (myClient[intNo].objSck.Connected == false)
                        {
                            break;
                        }
                    }
                    if (intNo < myClient.GetLength(0))
                    {
                        // クライアント送受信オブジェクト生成
                        myClient[intNo] = new ClientTcpIp();
                        myClient[intNo].intNo = intNo + 1;
                        myClient[intNo].objSck = myTcpClient;
                        myClient[intNo].objStm = myTcpClient.GetStream();
                        // クライアントとの送受信開始
                        Thread myClientThread = new Thread(
                            new ThreadStart(myClient[intNo].ReadWrite));
                        myClientThread.Start();
                    }
                    else
                    {
                        // 接続拒否
                        myTcpClient.Close();
                    }
                }
            }
            catch { }
        }

        // クライアント送受信クラス
        public class ClientTcpIp
        {
            public int intNo;
            public TcpClient objSck;
            public NetworkStream objStm;

            // クライアント送受信スレッド
            public void ReadWrite()
            {
                try
                {
                    while (true)
                    {                      

                        // ソケット受信
                        Byte[] rdat = new Byte[1024];
                        int ldat = objStm.Read(rdat, 0, rdat.GetLength(0));
                        if (ldat > 0)
                        {
                            // クライアントからの受信データ有り
                            // 送信データ作成
                            Byte[] sdat = new Byte[ldat];
                            Array.Copy(rdat, sdat, ldat);
                            String msg = "(" + intNo + ")" +
                                System.Text.Encoding.GetEncoding(
                                    "SHIFT-JIS").GetString(sdat);

                            // MessageBox.Show(msg, "クライアントからの受信結果");

                            rcvMsg = msg;

                            _iLog.Info("teleglam=" + msg);

                            // なにも返さない。
                            //sdat = System.Text.Encoding.GetEncoding("SHIFT-JIS").GetBytes(msg);
                            //objStm.Write(sdat, 0, sdat.GetLength(0)); // ソケット送信

                        }
                        else
                        {
                            // ソケット切断有り
                            // ソケットクローズ
                            objStm.Close();
                            objSck.Close();
                            return;
                        }
                    }
                }
                catch { }
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            txtRecv.Text = "";
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!rcvMsg.Equals(string.Empty)) 
            {
                txtRecv.Text += rcvMsg + "\r\n";
                rcvMsg = string.Empty;
            }
        }
    }
}
