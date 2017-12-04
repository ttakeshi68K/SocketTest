using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;

namespace testSocketClient
{
    public partial class fmSocketClient : Form
    {

        // ソケット生成
        private TcpClient objSck = new System.Net.Sockets.TcpClient();
        private NetworkStream objStm;


        public fmSocketClient()
        {
            InitializeComponent();
        }

        private void fmSocketClient_Load(object sender, EventArgs e)
        {
            // ソケット接続
            objSck.Connect("127.0.0.1", 30000);
            // ソケットストリーム取得
            objStm = objSck.GetStream();

        }

        private void fmSocketClient_FormClosed(object sender, FormClosedEventArgs e)
        {
            // ソケットクローズ
            objStm.Close();
            objSck.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {       
            // ソケット送信
            Byte[] dat =
                System.Text.Encoding.GetEncoding("SHIFT-JIS").GetBytes("abcあいう");
            objStm.Write(dat, 0, dat.GetLength(0));
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            // ソケット受信
            if (objSck.Available > 0)
            {
                Byte[] dat = new Byte[objSck.Available];
                objStm.Read(dat, 0, dat.GetLength(0));
                MessageBox.Show(
                    System.Text.Encoding.GetEncoding("SHIFT-JIS").GetString(dat), "サーバからの受信結果");
            }

        }
    }
}
