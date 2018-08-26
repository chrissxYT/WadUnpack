using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using static System.Console;

namespace WadUnpack
{
    public class Program
    {
        public static void Main(string[] args)
        {
            SetWindowSize(70, 30);

		    reader r = new reader(args[0]);

            WriteLine($"File Size:         {r.format_size()}");
            WriteLine($"Directory Entries: {r.format_entry_count()}");
            WriteLine($"Directory Offset:  {r.offset}");

		    List<entry> dir = new List<entry>();

            for (int z = 0; z < r.entries; z++)
            {
		    	entry d = new entry(r);
                dir.Add(d);
		    }

            byte ct = 7;

		    WriteLine("-----------------------------");
            string basedir = "unpacked_" + Path.GetFileNameWithoutExtension(args[0]);
            Directory.CreateDirectory(basedir);
		    WriteLine($@"Unpacking into {basedir}\...");
            WriteLine("-----------------------------");
            foreach (entry d in dir)
            {
                if(ct > 28)
                {
                    SetCursorPosition(0, 7);
                    ct = 7;
                }
                if (d.type != 67)
                    continue;
                else
                    unpack_texture(r, d, basedir + "\\" + d.name + ".png");
                ct++;
		    }
		    WriteLine("...Done.");
            for (byte b = ct; b < 28; b++)
                WriteLine("");
            Read();
	    }

	    static void unpack_texture(reader r, entry d, string dest)
        {
            r.index = d.offset;
            r.read_string(16);
		    int w = r.read32();
		    int h = r.read32();
            int pixels = w * h;
		    int[] offsets = new int[] { r.read32(), r.read32(), r.read32(), r.read32() };

		    int[] texture = new int[pixels];
            r.index = d.offset + offsets[0];
		    for(int z = 0; z < pixels; z++)
                texture[z] = r.read8();

            r.index = d.offset + offsets[3] + ((w/8) * (h/8)) + 2;
		    Color[] clut = new Color[256];
		    for(int z = 0; z < 256; z++)
                clut[z] = Color.FromArgb(0xFF, r.read8(), r.read8(), r.read8());

		    Bitmap img = new Bitmap(w, h, PixelFormat.Format32bppArgb);

		    for(int x = 0; x < w; x++)
			    for(int y = 0; y < h; y++)
				    img.SetPixel(x, y, clut[texture[x + (y * w)]]);

		    WriteLine(dest + "         ");
            img.Save(dest, ImageFormat.Png);
	    }
    }

    class entry
    {
        public int offset;
        public int size;
        public byte type;
        public string name;

        public entry(reader r)
        {
            offset = r.read32();
            size = r.read32();
            r.read32();
            type = r.read8();
            
            if (r.read8() != 0)
            {
                WriteLine("Compressed textures are not supported.");
                Read();
                Environment.Exit(1);
            }

            r.read8();
            r.read8();
            name = r.read_string(16).Trim()
                .Replace("<", "&lt;")
                .Replace(">", "&rt;")
                .Replace(":", "&dd;")
                .Replace("\"", "&qt;")
                .Replace("/", "&fs;")
                .Replace("\\", "&bs;")
                .Replace("|", "&vl;")
                .Replace("?", "&qm;")
                .Replace("*", "&sr;")
                .Replace("\u0000", "");
        }

        public override string ToString() => $"{offset}\t{name} ({size} bytes)";
    }

    class reader
    {
        public int index = 0;
        byte[] data;
        public int entries;
        public int offset;

        public reader(string file)
        {
            data = File.ReadAllBytes(file);

            string m = read_string(4);

            if (m == "WAD3" || m == "WAD2")
            {
                WriteLine(m + " format");
            }
            else
            {
                WriteLine("Not a WAD file.");
                Read();
                Environment.Exit(1);
            }

            entries = read32();
            offset = read32();
        }

        public string format_size()
        {
            long l = data.Length;
            return (l < 1000 ? l + "B" : l < 1000000 ? (l / 1000F).ToString("F") + "kB" : (l / 1000000F).ToString("F") + "MB");
        }

        public string format_entry_count()
        {
            return (entries < 1000 ? entries + "E" : (float)entries / 1000 + "kE");
        }

        public string read_string(int len)
        {
            string s = "";
            for (int i = 0; i < len; i++)
                s += (char)read8();
            return s;
        }

        public byte read8()
        {
            return data[index++];
        }

        public int read32()
        {
            return read8() | (read8() << 8) | (read8() << 16) | (read8() << 24);
        }
    }
}
