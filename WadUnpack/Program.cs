using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using static System.Console;
using static System.IO.Path;
using static System.IO.Directory;
using static System.IO.File;
using static System.Environment;
using static System.Drawing.Color;
using System.Text;

namespace WadUnpack
{
    public class Program
    {
        public static void Main(string[] args)
        {
            SetWindowSize(70, 30);

            string wadfile;
            if (args.Length > 0)
                wadfile = args[0];
            else
            {
                Write("WAD file: ");
                wadfile = ReadLine();
            }

            string basedir = "unpacked_" + GetFileNameWithoutExtension(wadfile);
		    reader r = new reader(wadfile);
            CreateDirectory(basedir);

#if DEBUG
            WriteLine("File Size:         " + r.size());
            WriteLine("Directory Entries: " + r.entry_count());
            WriteLine("Directory Offset:  " + r.offset);
#endif

		    List<entry> dir = new List<entry>();

            for (int j = 0; j < r.entries; j++)
                dir.Add(new entry(r));

            int i = 7;

		    WriteLine("---------------------------------");
		    WriteLine($"Unpacking into {basedir}\\...");
            WriteLine("---------------------------------");
            foreach (entry e in dir)
            {
                if(i > 28)
                {
                    SetCursorPosition(0, 7);
                    i = 7;
                }
                int j = e.type;
                if (j != 67)
#if DEBUG
                    WriteLine($"Cannot unpack an entry of type {hex(j, 2)}, skipping {{{e.dbgstr()}}}.");
#else
                    ;
#endif
                else
                    unpack_texture(r, e, Combine(basedir, e.name + ".png"));
                i++;
		    }
		    WriteLine("...Done.");
            for (; i < 28; i++)
                WriteLine();
            Read();
	    }

        static string hex(int i, int d)
        {
            return i.ToString("x" + d);
        }

	    static void unpack_texture(reader r, entry d, string dest)
        {
            r.seek(d.offset + 16);
		    int w = r.read32();
		    int h = r.read32();
            int pixels = w * h;
            int offset1 = r.read32();
            r.skip(8);
            int offset4 = r.read32();

		    int[] texture = new int[pixels];
            r.seek(d.offset + offset1);
		    for (int i = 0; i < pixels; i++)
                texture[i] = r.read8();

            r.seek(d.offset + offset4 + pixels / 64 + 2);
		    Color[] clut = new Color[256];
		    for(int i = 0; i < 256; i++)
                clut[i] = r.readc();

		    Bitmap img = new Bitmap(w, h);

		    for (int x = 0; x < w; x++)
			    for (int y = 0; y < h; y++)
				    img.SetPixel(x, y, clut[texture[x + y * w]]);

		    WriteLine(truncate(dest + "                                          ", 69));
            img.Save(dest, ImageFormat.Png);
	    }

        static string truncate(string s, int len)
        {
            return s == null || s.Length < len ? s : s.Substring(0, len);
        }
    }

    class entry
    {
        public int offset;
        public int size;
        public int type;
        public int compression;
        public string name;

        public entry(reader r)
        {
            offset = r.read32();
            size = r.read32();
            r.skip(4);
            type = r.read8();
            compression = r.read8();
            
            if (compression != 0)
            {
                WriteLine($"Compressed textures (compression: {compression.ToString("x2")}) are not supported.");
                Read();
                Exit(1);
            }

            r.skip(2);
            name = cutofffromfirst(r.reads(16), '\0');
        }

        static string cutofffromfirst(string s, char c)
        {
            return s.Substring(0, s.IndexOf(c));
        }

        public override string ToString() => $"{offset}\t{name} ({size} bytes)";

        public string dbgstr()
        {
            return hex(offset, 8) + hex(size, 8) + hex(type, 2) + hex(compression, 2) + name;
        }

        static string hex(int i, int d)
        {
            return i.ToString("x" + d);
        }
    }

    class reader
    {
        int index = 0;
        byte[] data;
        public int entries;
        public int offset;

        public reader(string file)
        {
            data = ReadAllBytes(file);

            string m = reads(4);

            if (m == "WAD3" || m == "WAD2")
                WriteLine(m + " format");
            else
            {
                Error.WriteLine("Not a WAD file.");
                Exit(1);
            }

            entries = read32();
            offset = read32();
        }

        public string size()
        {
            int l = data.Length;
            return l < 1000 ? l + "B" : l < 1000000 ? (l / 1000D).ToString("F2") + "kB" : (l / 1000000D).ToString("F2") + "MB";
        }

        public string entry_count()
        {
            return entries < 1000 ? entries + "E" : (entries / 1000D).ToString("F2") + "kE";
        }

        public string reads(int len)
        {
            if(len > 10)
            {
                StringBuilder s = new StringBuilder();
                for (int i = 0; i < len; i++)
                    s.Append((char)read8());
                return s.ToString();
            }
            else
            {
                string s = "";
                for (int i = 0; i < len; i++)
                    s += (char)read8();
                return s;
            }
        }

        public string readsz()
        {
            byte i;
            StringBuilder s = new StringBuilder();
            while ((i = data[index++]) != 0)
            {
                s.Append((char)i);
            }
            return s.ToString();
        }

        public int read8()
        {
            return index < data.LongLength ? data[index++] : -1;
        }

        public int read32()
        {
            return read8() | (read8() << 8) | (read8() << 16) | (read8() << 24);
        }

        public Color readc()
        {
            return FromArgb(data[index++], data[index++], data[index++]);
        }

        public void seek(int idx)
        {
            index = idx;
        }

        public void skip(int count)
        {
            index += count;
        }
    }
}
