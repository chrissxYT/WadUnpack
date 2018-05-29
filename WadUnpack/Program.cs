﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using static System.Console;

namespace WadUnpack
{
    public class Program
    {
        static int index = 0;
        static byte[] data;

        public static void Main(string[] args)
        {
            SetWindowSize(70, 30);

            string file = args[0];
		    data = File.ReadAllBytes(file);

            string m = read_string(4);
		    if (m == "WAD3" || m == "WAD2")
                WriteLine(m + " format");
		    else
            {
			    WriteLine("Not a WAD file.");
                Read();
			    Environment.Exit(1);
		    }

            int dirEntries = read32();
            int dirOffset = read32();

            WriteLine($"File Size:         {(data.Length < 1024 ? data.Length + "B" : data.Length < 1024*1024 ? (float)data.Length / 1024 + "KiB" : (float)data.Length / 1024 / 1024 + "MiB")}");
            WriteLine($"Directory Entries: {(dirEntries < 1000 ? dirEntries + "E" : (float)dirEntries / 1000 + "kE")}");
            WriteLine($"Directory Offset:  {dirOffset}");

            index = dirOffset;
		    List<DirectoryEntry> dir = new List<DirectoryEntry>();
		    for(int z = 0; z < dirEntries; z++)
            {
		    	DirectoryEntry d = new DirectoryEntry();
                dir.Add(d);
		    }

            byte ct = 7;

		    WriteLine("-----------------------------");
            string basedir = "unpacked_" + Path.GetFileNameWithoutExtension(file);
            Directory.CreateDirectory(basedir);
		    WriteLine($@"Unpacking into {basedir}\...");
            WriteLine("-----------------------------");
            foreach (DirectoryEntry d in dir)
            {
                if(ct > 28)
                {
                    SetCursorPosition(0, 7);
                    ct = 7;
                }
                if (d.type != 67)
                    continue;
                else
                    unpack_texture(d, basedir + "\\" + d.name + ".png");
                ct++;
		    }
		    WriteLine("...Done.                                                       ");
            for (byte b = ct; b < 28; b++)
                WriteLine("                                                       ");
            Read();
	    }

	    static void unpack_texture(DirectoryEntry d, string dest)
        {
            index = d.offset;
            read_string(16);
		    int w = read32();
		    int h = read32();
            int pixels = w * h;
		    int[] offsets = new int[] { read32(), read32(), read32(), read32() };

		    int[] texture = new int[pixels];
            index = d.offset + offsets[0];
		    for(int z = 0; z < pixels; z++)
                texture[z] = read8();
		
		    index = d.offset + offsets[3] + ((w/8) * (h/8)) + 2;
		    Color[] clut = new Color[256];
		    for(int z = 0; z < 256; z++)
                clut[z] = Color.FromArgb(0xFF, read8(), read8(), read8());

		    Bitmap img = new Bitmap(w, h, PixelFormat.Format32bppArgb);

		    for(int x = 0; x < w; x++)
			    for(int y = 0; y < h; y++)
				    img.SetPixel(x, y, clut[texture[x + (y * w)]]);

		    WriteLine(dest + "         ");
            img.Save(dest, ImageFormat.Png);
	    }

	    public static string read_string(int len)
        {
            string s = "";
            for (int i = 0; i < len; i++)
                s += (char)read8();
            return s;
        }

        public static byte read8() => (byte)(data[index++] & 0xFF);

        public static int read32() => read8() | (read8() << 8) | (read8() << 16) | (read8() << 24);
    }

    class DirectoryEntry
    {
        public int offset;
        public int size;
        public byte type;
        public string name;

        public DirectoryEntry()
        {
            offset = Program.read32();
            size = Program.read32();
            Program.read32();
            type = Program.read8();
            bool compression = Program.read8() != 0;
            Program.read8();
            Program.read8();
            name = Program.read_string(16).Trim().Replace("<", "&lt;").Replace(">", "&rt;").Replace(":", "&dd;").Replace("\"", "&qt;").Replace("/", "&fs;").Replace("\\", "&bs;").Replace("|", "&vl;").Replace("?", "&qm;").Replace("*", "&sr;").Replace("\u0000", "");
            if (compression)
            {
                WriteLine("Compressed textures are currently not supported.");
                Read();
                Environment.Exit(1);
            }
        }

        public override string ToString() => $"{offset}\t{name} ({size} bytes)";
    }
}
