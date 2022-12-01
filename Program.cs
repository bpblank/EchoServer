using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;


namespace EchoServer
{
    //多路复用select 服务端
    //同时检测多个信号
    //基本流程
    /*
    1. 初始化监听服务端Socket并绑定IP和端口
    2. 初始化客户端列表（存储服务端 以及客户端Socket）
    3. 碰见服务端socket，则开始接收客户端信号，并存储当前客户端socket
    4. 遇到客户端socket，则开始处理


    */
    /// <summary>
    /// 存储用户信息类。
    /// 包含用户socket以及用户携带信息
    /// </summary>
    public class ClientState
    {
        public Socket socket;
        public byte[] readBuff = new byte[1024];

    }
    internal class MainClass
    {
        static Socket listenedScoket;
        static Dictionary<Socket, ClientState> clientsInfo = new Dictionary<Socket, ClientState>();

        static void Main(string[] args)
        {
            
            Console.WriteLine("多路复用服务器");

            //初始化listenedScoket
            listenedScoket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress iPAddress = IPAddress.Parse("127.0.0.1");
            IPEndPoint iPEndPoint = new IPEndPoint(iPAddress, 8888);
            listenedScoket.Bind(iPEndPoint);
            listenedScoket.Listen(0);
            Console.WriteLine("服务器启动成功");


            //检查列表
            List<Socket> checkRead = new List<Socket>();

            while (true)
            {
                //补充服务端客户端检查列表
                checkRead.Clear();
                //添加服务端socket
                checkRead.Add(listenedScoket);
                //添加当前客户端响应socket
                foreach (ClientState s in clientsInfo.Values)
                {
                    checkRead.Add(s.socket);
                }

                //多路复用Select,检查待检查socket列表，保留可读socket，同时修改列表
                Socket.Select(checkRead, null, null, 1000);
                foreach (Socket sk in checkRead)
                {
                    if (sk == listenedScoket)
                    {
                        Console.WriteLine("服务端口检查并开始accept");
                        ReadListenedScoket(sk);
                        Console.WriteLine("长度为：" + clientsInfo.Count.ToString());
                    }
                    else
                    {
                        ReadClientsScoket(sk);
                    }
                }
            }
        }
        /// <summary>
        /// 监听Socket开始accept客户端
        /// 应答客户端并添加客户端信息
        /// </summary>
        /// <param name="s"></param>
        static void ReadListenedScoket(Socket s)
        {
            Console.WriteLine("开始接收Accept");
            //客户端响应socket，可以开始接收数据后续用clientAccept进行receive接收数据
            Socket clientAccept = s.Accept();
            ClientState clientState = new ClientState();
            clientState.socket = clientAccept;
            clientsInfo.Add(clientAccept, clientState);
        }

        /// <summary>
        /// 处理客户端信息
        /// </summary>
        /// <param name="clientSk"></param>
        public static void ReadClientsScoket(Socket clientSk)
        {
            //客户端信息
            ClientState state = clientsInfo[clientSk];
            int count = 0;
            try
            {
                count = clientSk.Receive(state.readBuff);
            }
            catch (Exception)
            {

                clientSk.Close();
                clientsInfo.Remove(clientSk);
                Console.WriteLine("客户端信息接收出错，关闭客户端");
                return;
            }
            //客户端无信息进行关闭
            if (count == 0)
            {
                clientSk.Close();
                clientsInfo.Remove(clientSk);
                Console.WriteLine("客户端无通信");
                return;
            }
            //对所有客户端进行广播信息
            string recvStr = System.Text.Encoding.UTF8.GetString(state.readBuff, 2, count-2);
            Console.WriteLine("接收信息为" + recvStr);
            //将信息转为byte进行传输
            //string sendStr = clientSk.RemoteEndPoint.ToString() + ":" + recvStr;
            //byte[] sendByte = System.Text.Encoding.Default.GetBytes(sendStr);
            byte[] sendBytes = new byte[count];
            Array.Copy(state.readBuff, 0, sendBytes, 0, count);
            foreach (ClientState cs in clientsInfo.Values)
            {
                cs.socket.Send(sendBytes);
            }
            return;
        }
    }
}
