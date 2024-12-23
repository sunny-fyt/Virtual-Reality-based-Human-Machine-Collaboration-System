using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

 
namespace ConsoleApp2
{
    public class Program
    {
        static int k = 1;
        static Socket socketSend;
        private static bool isReceived = false;
        public Program() {
            socketSend = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ip = IPAddress.Parse("192.168.31.117");
            IPEndPoint point = new IPEndPoint(ip, Convert.ToInt32("31001"));

            socketSend.Connect(point);

            Console.WriteLine("connect success!");
            //开启新的线程，不停的接收服务器发来的消息
            Thread c_thread = new Thread(Received);
            c_thread.IsBackground = true;
            c_thread.Start();

        }

        public void move(double v, double w) {
            //v是线速度，w是角速度
            //实现以指定的线速度和角速度前进
            //创建客户端Socket，获得远程ip和端口号


            //Console.WriteLine("please send information to server：");

            string myname = "name" + k;
            //string msg = "/api/robot_info";
            string msg = "/api/joy_control?angular_velocity="+w+"&linear_velocity="+v+"";//Console.ReadLine();
            byte[] buffer = new byte[1024 * 1024 * 3];
            buffer = Encoding.UTF8.GetBytes(msg);

            socketSend.Send(buffer);

        }
        public int getK() {
            //v是线速度，w是角速度
            //实现以指定的线速度和角速度前进
            //创建客户端Socket，获得远程ip和端口号


            //Console.WriteLine("please send information to server：");

            return k;
        }
        public void backtoMark(string name) {
            //标记现在的位置

            //实现以指定的线速度和角速度前进
            //创建客户端Socket，获得远程ip和端口号




            string myname = "name" + (k-1);
            string msg = "/api/move?marker="+myname;
            //string msg = "/api/joy_control?angular_velocity="+w+"&linear_velocity="+v+"";//Console.ReadLine();
            Console.WriteLine(msg);
            byte[] buffer = new byte[1024 * 1024 * 3];
            buffer = Encoding.UTF8.GetBytes(msg);

            socketSend.Send(buffer);

        }
        public void remark() {
            //标记现在的位置

            //实现以指定的线速度和角速度前进
            //创建客户端Socket，获得远程ip和端口号



            if (k != 1)
            {
                deletemark();
            }
            string myname = "name" + k;
            string msg = "/api/markers/insert?name=" + myname;
            //string msg = "/api/markers/insert?name=205_room" ;
            Console.WriteLine(msg);
            //string msg = "/api/joy_control?angular_velocity="+w+"&linear_velocity="+v+"";//Console.ReadLine();
            byte[] buffer = new byte[1024 * 1024 * 3];
            buffer = Encoding.UTF8.GetBytes(msg);

            socketSend.Send(buffer);
            k++;

        }
        public void deletemark()
        {
            //标记现在的位置

            //实现以指定的线速度和角速度前进
            //创建客户端Socket，获得远程ip和端口号




            string myname = "name" + (k-1);
            string msg = "/api/markers/delete?name=" + myname;
            //string msg = "/api/markers/insert?name=205_room" ;
            Console.WriteLine(msg);
            //string msg = "/api/joy_control?angular_velocity="+w+"&linear_velocity="+v+"";//Console.ReadLine();
            byte[] buffer = new byte[1024 * 1024 * 3];
            buffer = Encoding.UTF8.GetBytes(msg);

            socketSend.Send(buffer);
            k--;

        }

        public void forwordtoDANIU() {
            //标记现在的位置

            //实现以指定的线速度和角速度前进
            //创建客户端Socket，获得远程ip和端口号




            string myname = "name" + k;
            string msg = "/api/move?marker=daniu";
            //string msg = "/api/joy_control?angular_velocity="+w+"&linear_velocity="+v+"";//Console.ReadLine();
            byte[] buffer = new byte[1024 * 1024 * 3];
            buffer = Encoding.UTF8.GetBytes(msg);

            socketSend.Send(buffer);



            
        }

        public void forword1M()
        {
            //创建客户端Socket，获得远程ip和端口号



            // Console.WriteLine("please send information to server：");


            string msg = "/api/joy_control?angular_velocity=0&linear_velocity=0.5";//Console.ReadLine();
            byte[] buffer = new byte[1024 * 1024 * 3];
            buffer = Encoding.UTF8.GetBytes(msg);
            for (int i = 0; i < 10; i++) {
                Thread.Sleep(200);
                //Console.WriteLine(i);
                socketSend.Send(buffer);
            }

        }
        public void turn90(int m)
        {//m=1左转，m-=0右转
            //创建客户端Socket，获得远程ip和端口号



            // Console.WriteLine("please send information to server：");

            double pi = Math.PI/4;
            string msg = null;
            if (m == 1) {  msg = "/api/joy_control?angular_velocity=" + pi + "&linear_velocity=0"; }
            else {  msg = "/api/joy_control?angular_velocity=" + (-1)*pi + "&linear_velocity=0"; }
            //string msg = "/api/joy_control?angular_velocity="+pi+"&linear_velocity=0";//Console.ReadLine();
            byte[] buffer = new byte[1024 * 1024 * 3];
            buffer = Encoding.UTF8.GetBytes(msg);
            for (int i = 0; i < 9; i++)
            {
                Thread.Sleep(200);
                //Console.WriteLine(i);
                socketSend.Send(buffer);
            }

        }
        public void turn180(int m)
        {//m=1左转，m-=0右转
            //创建客户端Socket，获得远程ip和端口号



            // Console.WriteLine("please send information to server：");

            double pi = Math.PI /4;
            string msg = null;
            if (m == 1) { msg = "/api/joy_control?angular_velocity=" + pi + "&linear_velocity=0"; }
            else { msg = "/api/joy_control?angular_velocity=" + (-1) * pi + "&linear_velocity=0"; }
            //string msg = "/api/joy_control?angular_velocity="+pi+"&linear_velocity=0";//Console.ReadLine();
            byte[] buffer = new byte[1024 * 1024 * 3];
            buffer = Encoding.UTF8.GetBytes(msg);
            for (int i = 0; i < 19; i++)
            {
                Thread.Sleep(200);
                //Console.WriteLine(i);
                socketSend.Send(buffer);
            }

        }


        static void Received()
        {
            while (true)
            {
                try
                {
                    byte[] buffer = new byte[1024 * 1024 * 3];
                    //实际接收到的有效字节数
                    int len = socketSend.Receive(buffer);
                    if (len == 0)
                    {
                        break;
                    }
                    string str = Encoding.UTF8.GetString(buffer, 0, len);
                    Console.WriteLine(socketSend.RemoteEndPoint + ":" + str);
                    isReceived = true;
                }
                catch { }
            }
        }
        public void setIsReceived(bool status)
        {
            isReceived = status;
        }
        public bool getIsReceived()
        {
            return isReceived;
        }



    }
  
}
