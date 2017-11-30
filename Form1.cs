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
    public partial class Form1 : Form
    {
        //送信クライアント数
        int CONNECT_CNT = 300;

        private Thread[] threadClient;

        delegate void dlgWriteText(string text);

        public Form1()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
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

                btnStart.Enabled = false;
            }
            catch (Exception ex)
            {
            }
        }

        private void ClientListen(object args)
        {
            TcpClient client = new TcpClient("127.0.0.1", int.Parse(txtPort.Text));

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
                        uniBytes = Encoding.Convert(ecSjis, ecUni, bytes);

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
            }
        }
        private void btnStop_Click(object sender, EventArgs e)
        {
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

            for (int cnt = 0; cnt < CONNECT_CNT; cnt++)
            {
                if (threadClient[cnt] != null)
                    threadClient[cnt].Abort();
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            CloseClient();
        }
    }
}
