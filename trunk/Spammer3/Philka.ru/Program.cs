﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using doru;
using System.Text.RegularExpressions;
using System.Web;

namespace Philka.ru
{
    class Program
    {
        static void Main(string[] args)
        {
            Spammer3.Setup();
            new Program();
        }
        //string passhash = "033e31d41981c4f7dd5ce89df0b6ecff";
        //string sessid = "758a32ddcfe106024b91e579899ceb81";
        public Program()
        {
            Spamm();
        }
        intA i = new intA("i.txt");
        private void Spamm()
        {
            TcpClient _TcpClient = new TcpClient("philka.ru", 80);
            _Socket = _TcpClient.Client;
            foreach (string s in _ListA)
            {
                try
                {
                    if (!_listPosted.Contains(s))
                    {
                        "sending".Trace();
                        _listPosted.Add(s);
                        byte[] _bytes = File.ReadAllBytes("1 Sended Post.html");
                        Helper.Replace(ref _bytes, "_name_", s, 1);
                        Helper.Replace(ref _bytes, "_message_", HttpUtility.UrlEncode(_message,Encoding.Default), 1);
                        Http.Length(ref _bytes);
                        _Socket.Send(_bytes.Save());
                        string s2 = Http.ReadHttp(_Socket).ToStr().Save();
                        _listPosted.Flush();
                    }
                }
                catch (IOException e) { e.Message.Trace(); Connect(); }
            }
        }
        Socket _Socket;
        ListA _listPosted = new ListA("postedlist.txt");
        ListA _ListA = new ListA("list.txt");            
        private void Populate()
        {
            Connect();
            for (; i < 9999999; i+=60)
            {
                try
                {
                    byte[] _bytes = File.ReadAllBytes("1 Sended.html");
                    Helper.Replace(ref _bytes, "_st_", i.ToString(), 1);
                    _Socket.Send(_bytes);
                    string s = Http.ReadHttp(_Socket).ToStr().Save();

                    MatchCollection ms = Regex.Matches(s, @"(?:(?:user\d+.html)|(?:showuser=\d+))"">(.+?)</a></strong>");

                    foreach (Match m in ms)
                    {
                        _ListA.Add(HttpUtility.HtmlDecode(m.Groups[1].Value)).Trace();
                    }
                    "".Trace();
                    _ListA.Flush();
                }
                catch { Connect(); }
            }            
        }

        private void Connect()
        {
            if (_Socket != null) _Socket.Close();
            TcpClient _TcpClient = new TcpClient("philka.ru", 80);
            _Socket = _TcpClient.Client;
        }



        public static string _message { get { return File.ReadAllText("../message.txt",Encoding.Default); } }
    }

}
