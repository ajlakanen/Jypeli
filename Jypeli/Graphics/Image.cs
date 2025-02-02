﻿
using System;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp.Processing;

using ColorConverter = System.Converter<Jypeli.Color, Jypeli.Color>;
// Ehkä vähän tyhmät viritelmät samannimisten luokkien ympärille...
using SImage = SixLabors.ImageSharp.Image;
using SXImage = SixLabors.ImageSharp.Image<SixLabors.ImageSharp.PixelFormats.Rgba32>;
using System.Numerics;

namespace Jypeli
{
    /// <summary>
    /// Kuvan skaalausasetus
    /// </summary>
    public enum ImageScaling
    {
        /// <summary>
        /// Lineaarinen interpolointi (Sumentaa/pehmentää)
        /// </summary>
        Linear,
        /// <summary>
        /// Lähin pikseli (Pikseligrafiikalle oikea valinta)
        /// </summary>
        Nearest
    }
    /// <summary>
    /// Kuva.
    /// </summary>
    public class Image
    {
        private string assetName;

        internal static string[] ImageExtensions { get; } = { ".png", ".jpg"}; // TODO: Kaikki päätteet joita ImageSharp tukee

        /// <summary>
        /// ImageSharpin raakakuva
        /// </summary>
        internal SXImage image;

        /// <summary>
        /// Kuvan kahva näytönohjaimessa
        /// </summary>
        internal uint handle;

        /// <summary>
        /// Onko kuvan dataa muutettu ja se pitää viedä uudestaan näytönohjaimelle
        /// </summary>
        internal bool dirty;

        private ImageScaling scaling;

        /// <summary>
        /// Kuinka kuvan kokoa skaalataan ruudulle piirrettäessä.
        /// </summary>
        public ImageScaling Scaling
        {
            get => scaling; 
            set 
            {
                scaling = value;
                Game.GraphicsDevice.UpdateTextureScaling(this); // TODO: Pitäisikö tämä tehdä samoin kuin datan muokkaus, eli vasta piirtovaiheessa?
            }
        }

        /// <summary>
        /// Leveys pikseleinä.
        /// </summary>
        public int Width
        {
            get { return image.Width; }
        }

        /// <summary>
        /// Korkeus pikseleinä.
        /// </summary>
        public int Height
        {
            get { return image.Height; }
        }

        /// <summary>
        /// Nimi.
        /// </summary>
        public string Name
        {
            get { return assetName; }
        }

        /// <summary>
        /// Kuvan koko
        /// </summary>
        public Vector Size { get => new Vector(Width, Height); }

        internal Image(int width, int height)
        {
            AssertDimensions(width, height);
            CreateNewTexture(width, height, Color.Black);
        }

        /// <summary>
        /// Luo kuvan StorageFile-oliosta.
        /// </summary>
        /// <param name="f"></param>
        public Image(StorageFile f) : this(f.Stream)
        {
        }

        internal Image(Stream s)
        {
            image = (SXImage)SImage.Load(s);
        }

        internal Image(string assetName)
        {
            image = (SXImage)SImage.Load(assetName);
            this.assetName = assetName;
        }

        internal Image()
        {

        }

        internal Image(SImage img)
        {
            image = (SXImage)img;
        }

        /// <summary>
        /// Luo uuden kuvan.
        /// </summary>
        /// <param name="width">Kuvan leveys</param>
        /// <param name="height">Kuvan korkeus</param>
        /// <param name="backColor">Kuvan taustaväri</param>
        public Image(double width, double height, Color backColor)
            : this((int)Math.Round(width), (int)Math.Round(height), backColor)
        {
        }

        /// <summary>
        /// Luo uuden kuvan.
        /// </summary>
        /// <param name="width">Kuvan leveys</param>
        /// <param name="height">Kuvan korkeus</param>
        /// <param name="color">Kuvan väri</param>
        public Image(int width, int height, Color color)
        {
            AssertDimensions(width, height);
            assetName = null;
            CreateNewTexture(width, height, color);
        }

        private void CreateNewTexture(int width, int height, Color color)
        {
            Rgba32 col = new Rgba32();
            col.R = color.RedComponent;
            col.G = color.GreenComponent;
            col.B = color.BlueComponent;
            col.A = color.AlphaComponent;

            image = new SXImage(width, height, col);
        }

        /// <summary>
        /// Kuvan yksittäisten pikselien indeksointiin
        /// </summary>
        /// <param name="row">Rivi</param>
        /// <param name="col">Sarake</param>
        /// <returns>Pikselin väri</returns>
        public Color this[int row, int col]
        {
            get
            {
                // Imagesharpin indeksointi menee x,y, kun taas yleisesti 2d taulukko on y,x
                Rgba32 color = image[col, row];
                return new Color(color.R, color.G, color.B, color.A);
            }
            set
            {
                Rgba32 color = new Rgba32();
                color.R = value.RedComponent;
                color.G = value.GreenComponent;
                color.B = value.BlueComponent;
                color.A = value.AlphaComponent;

                image[col, row] = color;
                dirty = true;
            }
        }

        /// <summary>
        /// Kuvan pikselit Color-taulukkona
        /// </summary>
        /// <param name="ox">siirtymä x-suunnassa vasemmasta ylänurkasta</param>
        /// <param name="oy">siirtymä y-suunnassa vasemmasta ylänurkasta</param>
        /// <param name="w">alueen leveys joka kopioidaan</param>
        /// <param name="h">lueen korkaus joka kopioidaan</param>
        /// <returns>pikselit Color-taulukkona</returns>
        public Color[,] GetData(int ox = 0, int oy = 0, int w = int.MaxValue, int h = int.MaxValue)
        {
            int ny = Height;
            if (h < ny) ny = h;
            if (Height < ny + oy) ny = Height - oy;
            int nx = Width;
            if (w < nx) nx = w;
            if (Width < nx + ox) nx = Width - ox;
            if (nx <= 0 || ny <= 0) return new Color[0, 0];

            Color[,] bmp = new Color[ny, nx];
            
            for (int i = oy; i < oy + ny; i++)
            {
                Span<Rgba32> row = image.GetPixelRowSpan(i);
                for (int j = ox; j < ox + nx; j++)
                {
                    bmp[i - oy, j - ox] = new Color(row[j].R, row[j].G, row[j].B, row[j].A);
                }
            }

            return bmp;
        }


        /// <summary>
        /// Asettaa kuvan pikselit Color-taulukosta
        /// </summary>
        /// <param name="bmp">taulukko josta pikseleitä otetaan</param>
        /// <param name="ox">siirtymä x-suunnassa vasemmasta ylänurkasta</param>
        /// <param name="oy">siirtymä y-suunnassa vasemmasta ylänurkasta</param>
        /// <param name="w">alueen leveys johon kopioidaan</param>
        /// <param name="h">lueen korkaus johon kopioidaan</param>
        /// <returns>pikselit Color-taulukkona</returns>
        public void SetData(Color[,] bmp, int ox = 0, int oy = 0, int w = int.MaxValue, int h = int.MaxValue)
        {
            int ny = bmp.GetLength( 0 );
            int nx = bmp.GetLength( 1 );
            if ( ny > Height ) ny = Height;
            if ( nx > Width ) nx = Height;
            if ( ny > h ) ny = h;
            if ( nx > w ) nx = w;
            if ( Height < ny + oy ) ny = Height - oy;
            if ( Width < nx + ox ) nx = Width - ox;
            if ( nx <= 0 || ny <= 0 ) return;

            // TODO: Onko indeksointioperaatio kuinka hidas/nopea verrattuna muihin tapoihin muokata kuvaa?
            // TODO: Testaa kaikki Set/GetData metodit...

            for (int iy = oy; iy < ny; iy++)
            {
                for (int ix = ox; ix < nx; ix++)
                {
                    this[iy, ix] = bmp[iy - oy, ix - ox];
                }
            }
            UpdateTexture();
        }

        /// <summary>
        /// Asettaa kuvan pikselit annetun tavutaulukon mukaan.
        ///
        /// Taulukon tavut luetaan järjestyksessä punainen, vihreä, sininen, läpinäkyvyys
        /// </summary>
        /// <param name="byteArr">Tavutaulukko</param>
        /// <param name="height">Kuvan leveys</param>
        /// <param name="width">Kuvan korkeus</param>
        public void SetData(byte[] byteArr, int height, int width)
        {
            image = SImage.LoadPixelData<Rgba32>(byteArr, height, width);
        }

        /// <summary>
        /// Asettaa kuvan pikselit annetun tavutaulukon mukaan.
        ///
        /// Taulukon tavut luetaan järjestyksessä punainen, vihreä, sininen, läpinäkyvyys
        /// </summary>
        /// <param name="byteArr"></param>
        public void SetData(byte[] byteArr)
        {
            SetData(byteArr, this.Height, this.Width);
        }

        /// <summary>
        /// Kuvan pikselit byte-taulukkona.
        /// Tavut ovat järjestyksessä punainen, vihreä, sininen, läpinäkyvyys.
        /// </summary>
        /// <returns>pikselit byte-taulukkona</returns>
        public byte[] GetByteArray()
        {
            image.TryGetSinglePixelSpan(out var pixelSpan);
            byte[] rgbaBytes = MemoryMarshal.AsBytes(pixelSpan).ToArray();

            return rgbaBytes;
        }

        /// <summary>
        /// Palalutetaan kuvan pikselit ARGB-uint[,] -taulukkona
        /// </summary>
        /// <param name="ox">siirtymä x-suunnassa vasemmasta ylänurkasta</param>
        /// <param name="oy">siirtymä y-suunnassa vasemmasta ylänurkasta</param>
        /// <param name="w">alueen leveys joka kopioidaan</param>
        /// <param name="h">lueen korkaus joka kopioidaan</param>
        /// <returns>Kuvan pikselit ARGB-taulukkona</returns>
        public uint[,] GetDataUInt( int ox = 0, int oy = 0, int w = int.MaxValue, int h = int.MaxValue )
        {
            int ny = Height;
            if ( h < ny ) ny = h;
            if ( Height < ny + oy ) ny = Height - oy;
            int nx = Width;
            if ( w < nx ) nx = w;
            if ( Width < nx + ox ) nx = Width - ox;
            if ( nx <= 0 || ny <= 0 ) return new uint[0, 0];

            uint[,] bmp = new uint[ny, nx];

            for (int i = oy; i < oy + ny; i++)
            {
                Span<Rgba32> row = image.GetPixelRowSpan(i);
                for (int j = ox; j < ox + nx; j++)
                {
                    bmp[i - oy, j - ox] = row[j].PackedValue;
                }
            }

            return bmp;
        }


        /// <summary>
        /// Palalutetaan kuvan pikselit ARGB-uint[][] -taulukkona
        /// </summary>
        /// <param name="ox">siirtymä x-suunnassa vasemmasta ylänurkasta</param>
        /// <param name="oy">siirtymä y-suunnassa vasemmasta ylänurkasta</param>
        /// <param name="w">alueen leveys joka kopioidaan</param>
        /// <param name="h">lueen korkaus joka kopioidaan</param>
        /// <returns>Kuvan pikselit ARGB-taulukkona</returns>
        public uint[][] GetDataUIntAA( int ox = 0, int oy = 0, int w = int.MaxValue, int h = int.MaxValue )
        {
            int ny = Height;
            if ( h < ny ) ny = h;
            if ( Height < ny + oy ) ny = Height - oy;
            int nx = Width;
            if ( w < nx ) nx = w;
            if ( Width < nx + ox ) nx = Width - ox;
            if ( nx <= 0 || ny <= 0 ) return Array.Empty<uint[]>();

            uint[][] bmp = new uint[ny][];

            for (int i = oy; i < oy + ny; i++)
            {
                bmp[i - oy] = new uint[nx];
                Span<Rgba32> row = image.GetPixelRowSpan(i);
                for (int j = ox; j < ox + nx; j++)
                {
                    bmp[i - oy][j - ox] = row[j].PackedValue;
                }
            }

            return bmp;
        }


        /// <summary>
        /// Asetetaan kuvan pikselit ARGB-uint taulukosta
        /// </summary>
        /// <param name="bmp">taulukko josta pikselit otetaan</param>
        /// <param name="ox">siirtymä x-suunnassa vasemmasta ylänurkasta</param>
        /// <param name="oy">siirtymä y-suunnassa vasemmasta ylänurkasta</param>
        /// <param name="w">alueen leveys johon kopioidaan</param>
        /// <param name="h">alueen korkeus johon kopioidaan</param>
        public void SetData( uint[,] bmp, int ox = 0, int oy = 0, int w = int.MaxValue, int h = int.MaxValue )
        {
            int ny = bmp.GetLength( 0 );
            int nx = bmp.GetLength( 1 );
            if ( ny > Height ) ny = Height;
            if ( nx > Width ) nx = Width;
            if ( ny > h ) ny = h;
            if ( nx > w ) nx = w;
            if ( Height < ny + oy ) ny = Height - oy;
            if ( Width < nx + ox ) nx = Width - ox;

            if ( nx <= 0 || ny <= 0 ) return;

            for (int iy = oy; iy < ny; iy++)
            {
                for (int ix = ox; ix < nx; ix++)
                {
                    this[iy, ix] = Color.UIntToColor(bmp[iy - oy, ix - ox]);
                }
            }
            UpdateTexture();
        }


        /// <summary>
        /// Asetetaan kuvan pikselit ARGB-uint taulukosta
        /// </summary>
        /// <param name="bmp">taulukko josta pikselit otetaan</param>
        /// <param name="ox">siirtymä x-suunnassa vasemmasta ylänurkasta</param>
        /// <param name="oy">siirtymä y-suunnassa vasemmasta ylänurkasta</param>
        /// <param name="w">alueen leveys johon kopioidaan</param>
        /// <param name="h">alueen korkeus johon kopioidaan</param>
        public void SetData( uint[][] bmp, int ox = 0, int oy = 0, int w = int.MaxValue, int h = int.MaxValue )
        {
            int ny = bmp.Length;
            int nx = bmp[0].Length;
            if ( ny > Height ) ny = Height;
            if ( nx > Width ) nx = Width;
            if ( ny > h ) ny = h;
            if ( nx > w ) nx = w;
            if ( nx <= 0 || ny <= 0 ) return;

            for (int iy = oy; iy < ny; iy++)
            {
                for (int ix = ox; ix < nx; ix++)
                {
                    this[iy, ix] = Color.UIntToColor(bmp[iy - oy][ix - ox]);
                }
            }
            UpdateTexture();
        }

        private static void AssertDimensions(int width, int height)
        {
            if ( width < 1 || height < 1 )
                throw new ArgumentException( String.Format( "Image dimensions must be at least 1 x 1! (given: {0} x {1}", width, height ) );
        }

        /// <summary>
        /// Luo kopion kuvasta
        /// </summary>
        /// <returns></returns>
        public Image Clone()
        {
            Image copy = new Image();
            copy.image = image.Clone();

            return copy;
        }

        /// <summary>
        /// Suorittaa annetun pikselioperaation koko kuvalle.
        /// </summary>
        /// <param name="operation">Aliohjelma, joka ottaa värin ja palauttaa värin</param>
        public void ApplyPixelOperation(ColorConverter operation)
        {
            Color[,] data = GetData();

            for (int i = 0; i < data.GetLength(0); i++)
            {
                for (int j = 0; j < data.GetLength(1); j++)
                {
                    data[i,j] = operation(data[i,j]);
                }
            }
            SetData(data);
            UpdateTexture();
        }

        private void UpdateTexture()
        {
            dirty = true;
        }

        #region static methods


        /// <summary>
        /// Lataa kuvan tiedostosta.
        /// </summary>
        /// <param name="path">Tiedoston polku päätteineen.</param>
        public static Image FromFile(string path)
        {
            Image img = new Image(path);
            return img;
        }

        /// <summary>
        /// Lataa kuvan tiedostovirrasta.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static Image FromStream(Stream stream)
        {
            return new Image(SImage.Load(stream));
        }

        /// <summary> 
        /// Lataa kuvan Internetistä. 
        /// </summary> 
        /// <param name="url">Kuvan URL-osoite</param> 
        /// <returns>Kuva</returns> 
        public static Image FromURL(string url)
        {
            var req = FileManager.Client.GetAsync(url);
            req.Wait();
            Image img = new Image(SImage.Load(req.Result.Content.ReadAsStream()));
            return img;
        }

        /// <summary>
        /// Luo tähtitaivaskuvan.
        /// </summary>
        /// <param name="width">Tekstuurin leveys.</param>
        /// <param name="height">Tekstuurin korkeus.</param>
        /// <param name="stars">Tähtien määrä.</param>
        /// <param name="transparent">Onko tausta läpinäkyvä vai ei (jolloin siitä tulee täysin musta)</param>
        /// <returns>Tekstuuri.</returns>
        public static Image CreateStarSky(int width, int height, int stars, bool transparent = false)
        {
            Image img = new Image(width, height, transparent ? Color.Transparent : Color.Black);

            // Random stars
            for (int j = 0; j < stars; j++)
            {
                int px = RandomGen.NextInt(0, width);
                int py = RandomGen.NextInt(0, height);

                int radius = RandomGen.NextInt(2, 10) / 2;
                Color starcolor = RandomGen.NextColor(Color.White, new Color(192, 192, 192, 255));

                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        if (x * x + y * y <= radius * radius && px + x > 0 && px + x < width && py + y > 0 && py + y < height)
                        {
                            img[py + y, px + x] = starcolor;
                        }

                    }
                }
            }
            return img;
        }

        /// <summary>
        /// Luo kuvan tekstistä.
        /// </summary>
        /// <param name="text">Teksti josta kuva luodaan</param>
        /// <param name="font">Fontti</param>
        /// <param name="textColor">Tekstin väri</param>
        /// <param name="backgroundColor">Tekstin taustaväri</param>
        /// <returns>Teksti kuvana</returns>
        public static Image FromText( string text, Font font, Color textColor, Color backgroundColor )
        {
            if ( text == null )
                text = "";
            
            var device = Game.GraphicsDevice;

            Vector textDims = font.MeasureSize(text);
            int textw = (textDims.X > 1) ? Convert.ToInt32(textDims.X) : 1;
            int texth = (textDims.Y > 1) ? Convert.ToInt32(textDims.Y) : 1;

            Rendering.IRenderTarget rt = Game.GraphicsDevice.CreateRenderTarget((uint)textw, (uint)texth);

            device.SetRenderTarget(rt);
            device.Clear(backgroundColor);

            Matrix4x4 ProjectionMatrix = Matrix4x4.CreateOrthographic(
                textw,
                texth,
                1, 2
            );

            Matrix4x4 temp = Graphics.ViewProjectionMatrix;
            Graphics.ViewProjectionMatrix = ProjectionMatrix;

            Renderer.DrawText(text, Vector.Zero + new Vector(0, texth/2), font, textColor, Vector.One);
            Graphics.CustomBatch.Flush(); // TODO: Joku DrawTextImmediately tms. Voiko tämä mennä jossain tilanteissa nyt pieleen?

            Graphics.ViewProjectionMatrix = temp;

            Image img = new Image(textw, texth);
            device.GetScreenContentsToImage(img);

            device.SetRenderTarget(null);

            return Flip(img);
        }

        /// <summary>
        /// Piirtää tekstiä kuvan päälle.
        /// </summary>
        /// <param name="img">Kuva jonka päälle piirretään</param>
        /// <param name="text">Piirrettävä teksti</param>
        /// <param name="position">Piirtokohta (origo keskellä kuvaa)</param>
        /// <param name="font">Fontti</param>
        /// <param name="textColor">Tekstin väri</param>
        /// <param name="backgroundColor">Tekstin taustaväri</param>
        /// <returns>Kuva tekstin kanssa</returns>
        public static Image DrawTextOnImage( Image img, string text, Vector position, Font font, Color textColor, Color backgroundColor )
        {
            if ( text == null )
                text = "";

            /*var spriteBatch = new SpriteBatch( Game.GraphicsDevice );
            var device = spriteBatch.GraphicsDevice;

            XnaV2 textDims = font.XnaFont.MeasureString( text );
            int textw = ( textDims.X > 1 ) ? Convert.ToInt32( textDims.X ) : 1;
            int texth = ( textDims.Y > 1 ) ? Convert.ToInt32( textDims.Y ) : 1;

            //RenderTarget2D rt = new RenderTarget2D( device, textw, texth, 1, device.DisplayMode.Format );
            RenderTarget2D rt = new RenderTarget2D( device, img.Width, img.Height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8 );

            //device.SetRenderTarget( 0, rt );
            device.SetRenderTarget( rt );
            device.Clear( ClearOptions.Target | ClearOptions.DepthBuffer, backgroundColor.AsXnaColor(), 1.0f, 0 );

            float xpos = 0.5f * ( img.Width - textw ) + (float)position.X;
            float ypos = 0.5f * ( img.Height - texth ) - (float)position.Y;

            spriteBatch.Begin();
            spriteBatch.Draw( img.XNATexture, rt.Bounds, XnaColor.White );
            font.XnaFont.DrawText(Graphics.FontRenderer, text, (new XnaV2(xpos, ypos)).ToSystemNumerics(), textColor.AsXnaColor().ToSystemDrawing());
            spriteBatch.End();

            //device.SetRenderTarget( 0, null );
            device.SetRenderTarget( null );


            //return new Image( rt.GetTexture() );
            return new Image( rt );*/
            return new Image(20,20);
        }

        /// <summary>
        /// Piirtää tekstiä kuvan päälle keskelle kuvaa.
        /// </summary>
        /// <param name="img">Kuva jonka päälle piirretään</param>
        /// <param name="text">Piirrettävä teksti</param>
        /// <param name="font">Fontti</param>
        /// <param name="textColor">Tekstin väri</param>
        /// <returns>Kuva tekstin kanssa</returns>
        public static Image DrawTextOnImage( Image img, string text, Font font, Color textColor )
        {
            return DrawTextOnImage( img, text, Vector.Zero, font, textColor, Jypeli.Color.Transparent );
        }

        //TODO: Ehkä mielummin CreateGradient...
        /// <summary>
        /// Luo pystysuuntaisen liukuväritetyn kuvan.
        /// </summary>
        /// <param name="imageWidth">kuvan leveys.</param>
        /// <param name="imageHeight">kuvan korkeus.</param>
        /// <param name="lowerColor">Alareunassa käytettävä väri.</param>
        /// <param name="upperColor">Yläreunassa käytettävä väri.</param>
        /// <returns>Väritetty kuva.</returns>
        public static Image FromGradient(int imageWidth, int imageHeight, Color lowerColor, Color upperColor)
        {
            Image img = new Image(imageWidth, imageHeight);

            for (int ver = 0; ver < imageHeight; ver++)
            {
                for (int hor = 0; hor < imageWidth; hor++)
                {
                    img[ver, hor] = Color.Lerp(lowerColor, upperColor, (float)ver / (float)imageHeight);
                }
            }

            return img;
        }

        /// <summary>
        /// Luo yksivärisen kuvan.
        /// </summary>
        /// <param name="width">Kuvan leveys.</param>
        /// <param name="height">Kuvan korkeus.</param>
        /// <param name="color">Kuvan väri.</param>
        /// <returns>Väritetty kuva.</returns>
        public static Image FromColor(int width, int height, Color color)
        {
            return new Image(width, height, color);
        }

        /// <summary>
        /// Peilaa kuvan X-suunnassa.
        /// </summary>
        /// <param name="image">Peilattava kuva.</param>
        /// <returns>Peilattu kuva.</returns>
        public static Image Mirror(Image image)
        {
            Image img = image.Clone();
            img.image.Mutate(x => x.Flip(FlipMode.Horizontal));
            return img;
        }

        /// <summary>
        /// Peilaa kuvat X-suunnassa.
        /// </summary>
        /// <param name="images">Peilattavat kuvat.</param>
        /// <returns>Peilatut kuvat.</returns>
        public static Image[] Mirror(Image[] images)
        {
            Image[] result = new Image[images.Length];
            for (int i = 0; i < images.Length; i++)
                result[i] = Mirror(images[i]);
            return result;
        }

        // TODO: Näissä on tyhmä nimi
        /// <summary>
        /// Peilaa kuvan Y-suunnassa.
        /// </summary>
        /// <param name="image">Peilattava kuva.</param>
        /// <returns>Peilattu kuva.</returns>
        public static Image Flip(Image image)
        {
            Image img = image.Clone();
            img.image.Mutate(x => x.Flip(FlipMode.Vertical));
            return img;
        }

        /// <summary>
        /// Peilaa kuvat Y-suunnassa.
        /// </summary>
        /// <param name="images">Peilattavat kuvat.</param>
        /// <returns>Peilatut kuvat.</returns>
        public static Image[] Flip(Image[] images)
        {
            Image[] result = new Image[images.Length];
            for (int i = 0; i < images.Length; i++)
                result[i] = Flip(images[i]);
            return result;
        }

        /// <summary>
        /// Yhditää kaksi kuvaa olemaan vierekkäin uudessa kuvassa.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static Image TileHorizontal(Image left, Image right)
        {
            int width = left.Width + right.Width;
            int height = Math.Max(left.Height, right.Height);

            SImage img = new SXImage(width, height);
            img.Mutate(o => o
                        .DrawImage(left.image, new Point(0, 0), 1f)
                        .DrawImage(right.image, new Point(left.Width, 0), 1f)
            );
            return new Image(img);

        }

        /// <summary>
        /// Yhdistää kaksi kuvaa olemaan päällekkäin uudessa kuvassa
        /// </summary>
        /// <param name="top"></param>
        /// <param name="bottom"></param>
        /// <returns></returns>
        public static Image TileVertical(Image top, Image bottom)
        {
            int width = Math.Max(top.Width, bottom.Width);
            int height = top.Height + bottom.Height;

            SImage img = new SXImage(width, height);
            img.Mutate(o => o
                        .DrawImage(top.image, new Point(0, 0), 1f)
                        .DrawImage(bottom.image, new Point(0, top.Height), 1f)
            );
            return new Image(img);
        }

        #endregion

        /// <summary>
        /// Leikkaa kuvasta palan ja palauttaa sen uutena kuvana
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        /// <returns></returns>
        public Image Area(int left, int top, int right, int bottom)
        {
            int width = right - left;
            int height = bottom - top;

            if ( width <= 0 ) throw new ArgumentException( "Left coordinate must be less than right coordinate" );
            if ( height <= 0 ) throw new ArgumentException( "Top coordinate must be less than bottom coordinate" );
            
            Color[,] data = new Color[height, width];

            for ( int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    data[j, i] = this[top + j, left + i];
                }
            }

            Image img = new Image(width, height);
            img.SetData(data);

            return img;
        }

        /// <summary>
        /// Täyttää kuvan värillä
        /// </summary>
        /// <param name="backColor"></param>
        public void Fill(Color backColor)
        {
            image = new SXImage(Width, Height, new Rgba32(backColor.ToUInt()));

            UpdateTexture();
        }

        /// <summary>
        /// Korvaa värin toisella värillä.
        /// </summary>
        /// <param name="src">Korvattava väri.</param>
        /// <param name="dest">Väri jolla korvataan.</param>
        /// <param name="tolerance">Kuinka paljon korvattava väri voi poiketa annetusta.</param>
        /// <param name="blend">Sävytetäänkö korvattavaa väriä sen mukaan kuinka kaukana se on alkuperäisestä väristä</param>
        /// <param name="exactAlpha">Vaaditaanko täsmälleen sama läpinäkyvyys ennen kuin korvataan</param>
        public void ReplaceColor(Color src, Color dest, double tolerance, bool blend, bool exactAlpha = false)
        {
            Color op(Color c)
            {
                if (exactAlpha && c.AlphaComponent != src.AlphaComponent)
                    return c;

                if (Color.Distance(c, src) <= tolerance)
                {
                    if (!blend)
                        return dest;
                    return Color.Mix(c, dest);
                }

                return c;
            }

            ApplyPixelOperation(op);
        }

        /// <summary>
        /// Korvaa värin toisella värillä.
        /// </summary>
        /// <param name="src">Korvattava väri</param>
        /// <param name="dest">Väri jolla korvataan</param>
        public void ReplaceColor(Color src, Color dest)
        {
            Color op(Color c)
            {
                return c == src ? dest : c;
            }

            ApplyPixelOperation(op);
        }

        /// <summary>
        /// Tallentaa kuvan jpg-muodossa
        /// </summary>
        /// <param name="path">Tiedoston nimi</param>
        public void SaveAsJpeg(string path)
        {
            image.SaveAsJpeg(path);
        }


        /// <summary>
        /// Tallentaa kuvan png-muodossa
        /// </summary>
        /// <param name="path">Tiedoston nimi</param>
        public void SaveAsPng(string path)
        {
            image.SaveAsPng(path);
        }
    }
}

