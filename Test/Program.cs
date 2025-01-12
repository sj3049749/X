﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NewLife;
using NewLife.Caching;
using NewLife.Core.Collections;
using NewLife.Http;
using NewLife.Log;
using NewLife.Net;
using NewLife.Reflection;
using NewLife.Remoting;
using NewLife.Security;
using NewLife.Serialization;
using XCode.Code;
using XCode.DataAccessLayer;
using XCode.Membership;
using XCode.Service;
#if !NET4
using TaskEx = System.Threading.Tasks.Task;
#endif

namespace Test
{
    public class Program
    {
        private static void Main(String[] args)
        {
            //XTrace.Log = new NetworkLog();
            XTrace.UseConsole();
#if DEBUG
            XTrace.Debug = true;
#endif
            while (true)
            {
                var sw = Stopwatch.StartNew();
#if !DEBUG
                try
                {
#endif
                    Test6();
#if !DEBUG
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex?.GetTrue());
                }
#endif

                sw.Stop();
                Console.WriteLine("OK! 耗时 {0}", sw.Elapsed);
                //Thread.Sleep(5000);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                var key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.C) break;
            }
        }

        static async void Test1()
        {
            var ip = "180.165.235.0".IPToAddress();
            Console.WriteLine(ip);

            var mi = new MachineInfo();

            foreach (var pi in mi.GetType().GetProperties())
            {
                Console.WriteLine("{0}:\t{1}", pi.Name, mi.GetValue(pi));
            }

            Console.ReadKey();
        }

        static void Test2()
        {
            //var uri = new Uri("http://www.newlifex.com");
            //var client = new TinyHttpClient();
            //var html = client.Send(uri, null)?.ToStr();

            //Console.WriteLine(client.BaseAddress);

            //Console.WriteLine(uri);
            //Console.WriteLine(client.BaseAddress == uri);

            var client = new HttpClient();
            client.BaseAddress = new Uri("http://feifan.link:2233");

            var rs = client.Invoke<Object>("api/info");
            Console.WriteLine(rs.ToJson(true));

            rs = client.Invoke<Object>("api/info3", rs);
            Console.WriteLine(rs.ToJson(true));
        }

        static void Test3()
        {
            //XTrace.WriteLine("IsConsole={0}", Runtime.IsConsole);
            //Console.WriteLine("IsConsole={0}", Runtime.IsConsole);
            //XTrace.WriteLine("MainWindowHandle={0}", Process.GetCurrentProcess().MainWindowHandle);

            if (Console.ReadLine() == "1")
            {
                var svr = new ApiServer(1234)
                //var svr = new ApiServer("http://*:1234")
                {
                    Log = XTrace.Log,
                    //EncoderLog = XTrace.Log,
                    StatPeriod = 10,
                };

                var ns = svr.EnsureCreate() as NetServer;
                ns.EnsureCreateServer();
                var ts = ns.Servers.FirstOrDefault(e => e is TcpServer);
                //ts.ProcessAsync = true;

                svr.Start();

                Console.ReadKey();
            }
            else
            {
                var client = new ApiClient("tcp://127.0.0.1:335,tcp://127.0.0.1:1234")
                {
                    Log = XTrace.Log,
                    //EncoderLog = XTrace.Log,
                    StatPeriod = 10,

                    UsePool = true,
                };
                client.Open();

                TaskEx.Run(() =>
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        for (var i = 0; i < 10; i++)
                        {
                            client.InvokeAsync<Object>("Api/All", new { state = 111 }).Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteException(ex.GetTrue());
                    }
                    sw.Stop();
                    XTrace.WriteLine("总耗时 {0:n0}ms", sw.ElapsedMilliseconds);
                });

                TaskEx.Run(() =>
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        for (var i = 0; i < 10; i++)
                        {
                            client.InvokeAsync<Object>("Api/All", new { state = 222 }).Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteException(ex.GetTrue());
                    }
                    sw.Stop();
                    XTrace.WriteLine("总耗时 {0:n0}ms", sw.ElapsedMilliseconds);
                });

                TaskEx.Run(() =>
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        for (var i = 0; i < 10; i++)
                        {
                            client.InvokeAsync<Object>("Api/Info", new { state = 333 }).Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteException(ex.GetTrue());
                    }
                    sw.Stop();
                    XTrace.WriteLine("总耗时 {0:n0}ms", sw.ElapsedMilliseconds);
                });

                TaskEx.Run(() =>
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        for (var i = 0; i < 10; i++)
                        {
                            client.InvokeAsync<Object>("Api/Info", new { state = 444 }).Wait();
                        }
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteException(ex.GetTrue());
                    }
                    sw.Stop();
                    XTrace.WriteLine("总耗时 {0:n0}ms", sw.ElapsedMilliseconds);
                });

                Console.ReadKey();
            }
        }

        static void Test4()
        {
            var v = Rand.NextBytes(32);
            Console.WriteLine(v.ToBase64());

            ICache ch = null;
            //ICache ch = new DbCache();
            //ch.Set(key, v);
            //v = ch.Get<Byte[]>(key);
            //Console.WriteLine(v.ToBase64());
            //ch.Remove(key);

            Console.Clear();

            Console.Write("选择要测试的缓存：1，MemoryCache；2，DbCache；3，Redis ");
            var select = Console.ReadKey().KeyChar;
            switch (select)
            {
                case '1':
                    ch = new MemoryCache();
                    break;
                case '2':
                    ch = new DbCache();
                    break;
                case '3':
                    ch = Redis.Create("127.0.0.1", 9);
                    break;
            }

            var mode = false;
            Console.WriteLine();
            Console.Write("选择测试模式：1，顺序；2，随机 ");
            if (Console.ReadKey().KeyChar != '1') mode = true;

            Console.Clear();

            ch.Bench(mode);
        }

        static void Test5()
        {
            var set = XCode.Setting.Current;
            set.Debug = true;
            set.ShowSQL = true;

            Console.WriteLine("1，服务端；2，客户端");
            if (Console.ReadKey().KeyChar == '1')
            {
                var n = UserOnline.Meta.Count;

                var svr = new DbServer
                {
                    Log = XTrace.Log,
                    StatPeriod = 5
                };
                svr.Start();
            }
            else
            {
                DAL.AddConnStr("net", "Server=tcp://admin:newlife@127.0.0.1:3305/Log", null, "network");
                var dal = DAL.Create("net");

                UserOnline.Meta.ConnName = "net";

                var count = UserOnline.Meta.Count;
                Console.WriteLine("count={0}", count);

                var entity = new UserOnline
                {
                    Name = "新生命",
                    OnlineTime = 12345
                };
                entity.Insert();

                Console.WriteLine("id={0}", entity.ID);

                var entity2 = UserOnline.FindByKey(entity.ID);
                Console.WriteLine("user={0}", entity2);

                entity2.Page = Rand.NextString(8);
                entity2.Update();

                entity2.Delete();

                for (var i = 0; i < 100; i++)
                {
                    entity2 = new UserOnline
                    {
                        Name = Rand.NextString(8),
                        Page = Rand.NextString(8)
                    };
                    entity2.Insert();

                    Thread.Sleep(5000);
                }
            }

            //var client = new DbClient();
            //client.Log = XTrace.Log;
            //client.EncoderLog = client.Log;
            //client.StatPeriod = 5;

            //client.Servers.Add("tcp://127.0.0.1:3305");
            //client.Open();

            //var db = "Membership";
            //var rs = client.LoginAsync(db, "admin", "newlife").Result;
            //Console.WriteLine((DatabaseType)rs["DbType"].ToInt());

            //var ds = client.QueryAsync("Select * from User").Result;
            //Console.WriteLine(ds);

            //var count = client.QueryCountAsync("User").Result;
            //Console.WriteLine("count={0}", count);

            //var ps = new Dictionary<String, Object>
            //{
            //    { "Logins", 3 },
            //    { "id", 1 }
            //};
            //var es = client.ExecuteAsync("update user set Logins=Logins+@Logins where id=@id", ps).Result;
            //Console.WriteLine("Execute={0}", es);
        }

        private static NetServer _netServer;
        static void Test6()
        {
            var svr = new NetServer<MySession>
            {
                Port = 12345,
                Log = XTrace.Log,
                SessionLog = XTrace.Log,
                SocketLog = XTrace.Log,
                LogSend = true,
                LogReceive = true,
            };
            svr.Start();

            _netServer = svr;
        }
        class MySession : NetSession
        {
            protected override void OnReceive(ReceivedEventArgs e)
            {
                base.OnReceive(e);

                Send(e.Packet);
            }
        }

        static void Test7()
        {
            Role.Meta.Session.Dal.Db.ShowSQL = true;
            Role.Meta.Session.Dal.Expire = 10;
            Role.Meta.Session.Dal.Db.Readonly = true;

            var list = Role.FindAll();
            Console.WriteLine(list.Count);

            Thread.Sleep(1000);

            list = Role.FindAll();
            Console.WriteLine(list.Count);

            Thread.Sleep(1000);

            var r = list.Last();
            r.IsSystem = !r.IsSystem;
            r.Update();

            Thread.Sleep(5000);

            list = Role.FindAll();
            Console.WriteLine(list.Count);
        }

        static void Test8()
        {
            var ss = new String[8];
            ss[1] = "Stone";
            ss[3] = "NewLife";
            var str = ss.Join();
            Console.WriteLine(str);
        }

        static async void Test9()
        {
            //var rds = new Redis();
            //rds.Server = "127.0.0.1";
            //if (rds.Pool is ObjectPool<RedisClient> pp) pp.Log = XTrace.Log;
            //rds.Bench();

            //Console.ReadKey();

            var svr = new ApiServer(3379)
            {
                Log = XTrace.Log
            };
            svr.Start();

            var client = new ApiClient("tcp://127.0.0.1:3379")
            {
                Log = XTrace.Log
            };
            client.Open();

            for (var i = 0; i < 10; i++)
            {
                XTrace.WriteLine("Invoke {0}", i);
                var sw = Stopwatch.StartNew();
                var rs = await client.InvokeAsync<String[]>("Api/All");
                sw.Stop();
                XTrace.WriteLine("{0}=> {1:n0}us", i, sw.Elapsed.TotalMilliseconds * 1000);
                //XTrace.WriteLine(rs.Join(","));
            }

            Console.WriteLine();
            Parallel.For(0, 10, async i =>
            {
                XTrace.WriteLine("Invoke {0}", i);
                var sw = Stopwatch.StartNew();
                var rs = await client.InvokeAsync<String[]>("Api/All");
                sw.Stop();
                XTrace.WriteLine("{0}=> {1:n0}us", i, sw.Elapsed.TotalMilliseconds * 1000);
                //XTrace.WriteLine(rs.Join(","));
            });
        }

        static void Test10()
        {
            var dt1 = new DateTime(1970, 1, 1);
            //var x = dt1.ToFileTimeUtc();

            var yy = Int64.Parse("-1540795502468");

            //var yy = "1540795502468".ToInt();
            Console.WriteLine(yy);

            var dt = 1540795502468.ToDateTime();
            var y = dt.ToUniversalTime();
            Console.WriteLine(dt1.ToLong());
        }

        static void Test11()
        {
            var xmlFile = Path.Combine(Directory.GetCurrentDirectory(), "../X/XCode/Model.xml");
            var output = Path.Combine(Directory.GetCurrentDirectory(), "../");
            EntityBuilder.Build(xmlFile, output);
        }

        /// <summary>测试序列化</summary>
        static void Test12()
        {
            var bdic = new Dictionary<String, Object>
            {
                { "x", "1" },
                { "y", "2" }
            };

            var flist = new List<foo>
            {
                new foo() { A = 3, B = "e", AList = new List<String>() { "E", "F", "G" }, ADic = bdic }
            };

            var dic = new Dictionary<String, Object>
            {
                { "x", "1" },
                { "y", "2" }
            };


            var entity = new foo()
            {
                A = 1,
                B = "2",
                C = DateTime.Now,
                AList = new List<String>() { "A", "B", "C" },
                BList = flist,
                CList = new List<String>() { "A1", "B1", "C1" },
                ADic = dic,
                BDic = bdic
            };

            var json = entity.ToJson();

            var fentity = json.ToJsonEntity(typeof(foo));
        }
    }

    class foo
    {
        public Int32 A { get; set; }

        public String B { get; set; }

        public DateTime C { get; set; }

        public IList<String> AList { get; set; }

        public IList<foo> BList { get; set; }

        public List<String> CList { get; set; }

        public Dictionary<String, Object> ADic { get; set; }

        public IDictionary<String, Object> BDic { get; set; }
    }
}