﻿using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;

/*
 * Vizzi is a tool that can take ram.dat files that are generated by Benjojo's fork of bochs
 * and then make a collection of frames that can then be compiled (by using a tool like ffmpeg)
 * into a nice little film showing you where the RAM was being written to during the OS's boot.
 */

namespace vizzi
{
    class RamEvent 
    {
        public UInt64 time;
        public UInt32 addr;
        public UInt32 len;
    }


    class Program
    {
        const int MAX_RAM = 64 * 1024 * 1024;
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Vizzi is a tool that can vizualize ram.dat files that are generated by Benjojo's fork of bochs");
                Console.WriteLine("To use please input a ram.dat file.");
                Console.WriteLine("$ mono vizzi.exe ram.dat");
                Environment.Exit(0);
            }
            else 
            {
                if(File.Exists(args[0]))
                {
                    byte[] RawFile = ReadFile(args[0]);
                    RamEvent[] Events = ParseFile(RawFile);
                    int res = (int)Math.Ceiling(Math.Sqrt(MAX_RAM));
                    Bitmap WorkingPlace = new Bitmap(res, res);
                    Console.WriteLine("Frame Size is {0}x{0}", res);
                    Console.WriteLine("I have {0} events to process.",Events.Length);
                    int FrameTime = 1000 / 30; // 30 FPS
                    int FrameCount = 0;
                    UInt64 LastFrame = 0;
                    int EventCount = 0;

                    foreach (RamEvent e in Events)
                    {
                        ApplyEvent(WorkingPlace, e);
                        if ((LastFrame + (UInt64)FrameTime) < e.time)
                        {
                            LastFrame = e.time;
                            WorkingPlace.Save(string.Format("./{0}.png", FrameCount));
                            FrameCount++;
                            if (FrameCount % 10 == 0)
                            {
                                Console.WriteLine("I have processed {0} Frames. I'm {1}% or {2}/{3} though the events I have.", FrameCount, (float)((float)EventCount / (float)Events.Length) * 100, EventCount, Events.Length);
                            }
                        }
                        EventCount++;
                    }
                }
                else
                {
                    Console.WriteLine("That file seems to not exist. Exiting.");
                    Environment.Exit(1);
                }
            }
        }
        #region Image Processing

        static Bitmap ApplyEvent(Bitmap Current, RamEvent Event)
        {
            int x = (int)Event.addr % Current.Height;
            int y = (int)Event.addr / Current.Height;
            for (int i = 0; i < Event.len; i++)
            {
                Current.SetPixel(x, y, Color.Red);
                x++;
                if (x <= Current.Height) { x = 0; y++; }
            }
            return Current;
        }

        #endregion

        #region FileParse
        static RamEvent[] ParseFile(byte[] File)
        {
            List<RamEvent> EventList = new List<RamEvent>();

            // the length of each chunk of info is 16 bytes.
            // We need to check if we can divide by 16 in this file else we might of messed up
            // or even worse a user might be trying to put a .doc file into us.

            if(File.Length % 16 != 0) 
            {
                Console.WriteLine("WARNING. FILE IS NOT DIVIDABLE BY 16. WHAT I AM ABOUT TO DO IS PROABBLY NOT WHAT YOU WANT ME TO DO.");
                Console.WriteLine("Press enter if you really want me to do this.");
                Console.ReadLine();
            }

            byte[] Buffer = new byte[16];
            int p = 0;
            int len = File.Length;
            for(int i = 0; i < len; i++)
            {
                if(i % 16 == 0 && i != 0)
                {
                    EventList.Add(ParseChunk(Buffer));
                    p = 0;
                    for(int c = 0; c < 16; c++)
                    {
                        Buffer[c] = 0x00;
                    }
                }
                Buffer[p] = File[i];
                p++;
            }
            
            return EventList.ToArray();
        }

        static RamEvent ParseChunk(byte[] Chunk)
        {
            RamEvent Data = new RamEvent();

            Data.time = BitConverter.ToUInt64(Chunk, 0);
            Data.addr = BitConverter.ToUInt32(Chunk, 8);
            Data.len = BitConverter.ToUInt32(Chunk,12);

            return Data;
        }

        static byte[] ReadFile(string file) 
        {
            return File.ReadAllBytes(file);
        }
        #endregion
    }
}
