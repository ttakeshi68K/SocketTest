using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SocketTest
{
    public partial class FormServer : Form
    {
        //送信クライアント数

        private  static int CONNECT_CNT = 300;

        private ClientTcpIp[] myClient = new ClientTcpIp[CONNECT_CNT];

        // ソケット・リスナー
        private TcpListener myListener;

        private Thread[] threadClient;

        delegate void dlgWriteText(string text);

        public FormServer()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            StratSeverSockets();
            btnStart.Enabled = false;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            CleosSeverSockets();

            CloseClient();
            btnStart.Enabled = true;
        }

        private void WriteReadText(string text)
        {
            //受信文字の改行は全て↓に置き換え
            text = text.Replace("\r\n", "↓");

            txtReceive.AppendText(text + "\r\n");
        }
   
        private void CloseClient()
        {
            try
            {
                for (int cnt = 0; cnt < CONNECT_CNT; cnt++)
                {
                    if (threadClient[cnt] != null)
                        threadClient[cnt].Abort();
                }
            }
            catch (NullReferenceException ex)
            {
                //  スレッドの無い時のNullReferenceExceptionは無視
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            CloseClient();
        }

        // サーバーソケット接続開始処理

        private void StratSeverSockets()
        {
            // IPアドレス＆ポート番号設定
            int myPort = int.Parse(txtPort.Text);
            IPAddress myIp = Dns.GetHostEntry("localhost").AddressList[0];
            IPEndPoint myEndPoint = new IPEndPoint(myIp, myPort);

            // リスナー開始
            myListener = new TcpListener(myEndPoint);
            myListener.Start();

            // クライアント接続待ち開始
            Thread myServerThread = new Thread(new ThreadStart(this.ServerThread));
            myServerThread.Start();
        }


        // サーバーソケット終了処理

        private void CleosSeverSockets()
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

        // サーバ側クライアント接続待ちスレッド
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

        // クライアント 相手先サーバからのデータを送信開始

        private void clientListennr()
        {
            CONNECT_CNT = int.Parse(txtClientcnt.Text);
            try
            {
                threadClient = new Thread[CONNECT_CNT];

                for (int cnt = 0; cnt < CONNECT_CNT; cnt++)
                {
                    //サーバからのデータを受信するループをスレッドで処理
                    threadClient[cnt] = new Thread(new ParameterizedThreadStart(this.ClientListen));
                    threadClient[cnt].Start();
                }

                btnSend.Enabled = false;
            }
            catch (Exception ex)
            {
            }

        }


        //　クライアント サーバへの送信処理本体      
        private void ClientListen(object args)
        {
            try
            {
                TcpClient client = new TcpClient("127.0.0.1", int.Parse(txtSendPort.Text));

                NetworkStream stream = client.GetStream();

                Byte[] bytes = new Byte[100];

                dlgWriteText dlgText = new dlgWriteText(WriteReadText);

                try
                {
                    //sift-jisに変換して送る
                    Encoding ecSjis = Encoding.GetEncoding("shift-jis");
                    Byte[] data = ecSjis.GetBytes(txtSend.Text);

                    stream.Write(data, 0, data.Length);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("送信できませんでした。", "送信エラー");
                }

                while (true)
                {
                    try
                    {
                        int intCount = stream.Read(bytes, 0, bytes.Length);

                        if (intCount != 0)
                        {
                            //受信部分だけ切り出す
                            Byte[] getByte = new byte[intCount];
                            for (int i = 0; i < intCount; i++)
                                getByte[i] = bytes[i];
                            byte[] uniBytes;

                            //'S-Jisからユニコードに変換
                            Encoding ecSjis = Encoding.GetEncoding("shift-jis");
                            Encoding ecUni = Encoding.GetEncoding("utf-16");
                            uniBytes = Encoding.Convert(ecSjis, ecUni, getByte);

                            string strGetText = ecUni.GetString(uniBytes);

                            //受信文字を切り出す
                            strGetText = strGetText.Substring(0, strGetText.IndexOf((char)0));
                            txtReceive.Invoke(dlgText, strGetText);

                            //サーバと切断
                            if (client != null && client.Connected)
                                client.Close();
                        }
                        else
                        {
                            //サーバと切断
                            if (client != null && client.Connected)
                                client.Close();

                            return;
                        }
                    }
                    catch (System.Threading.ThreadAbortException)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        return;
                    }

                    btnSend.Enabled = true;
                }

            }
            catch (SocketException socketEx)
            {
                string msg = "ERROR SocketErrorCode=:" + socketEx.SocketErrorCode
                              + " ERROR NativeErrorCodee=:" + socketEx.NativeErrorCode
                                    + " Message=:" + socketEx.Message;

                MessageBox.Show(msg);

            }
            catch (Exception ex)
            {

            }


        }

        private void txtSend_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtReceive_TextChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void txtPort_TextChanged(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void txtSendPort_TextChanged(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void btnSend_Click_1(object sender, EventArgs e)
        {
            clientListennr();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtReceive.Clear();
        }
    }


    // サーバ用　クライアント送受信クラス
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
                    Byte[] rdat = new Byte[4096];
                    int intCount = objStm.Read(rdat, 0, rdat.Length);

                    if (intCount != 0)
                    {
                        // クライアントからの受信データ有り
                        // 送信データ作成
                        Byte[] sdat = new Byte[intCount];
                        Array.Copy(rdat, sdat, intCount);

                        byte[] uniBytes;

                        //'S-Jisからユニコードに変換
                        Encoding ecSjis = Encoding.GetEncoding("shift-jis");
                        Encoding ecUni = Encoding.GetEncoding("utf-16");
                        uniBytes = Encoding.Convert(ecSjis, ecUni, sdat);

                        string strGetText = ecUni.GetString(uniBytes);


                        String msg = "(" + intNo + ")" +
                            System.Text.Encoding.GetEncoding(
                                "SHIFT-JIS").GetString(sdat);


                        sdat = System.Text.Encoding.GetEncoding(
                            "SHIFT-JIS").GetBytes(msg);
                        // ソケット送信 受け取ったモノを送り返す。
                        // objStm.Write(sdat, 0, sdat.GetLength(0));
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



}
