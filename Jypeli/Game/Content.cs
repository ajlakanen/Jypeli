﻿#region MIT License
/*
 * Copyright (c) 2013 University of Jyväskylä, Department of Mathematical
 * Information Technology.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */
#endregion

/*
 * Authors: Tero Jäntti, Tomi Karppinen, Janne Nikkanen, Mikko Röyskö.
 */

using System;
using System.IO;
using Jypeli.Content;



namespace Jypeli
{
    public partial class Game
    {
        /// <summary>
        /// Mediasoitin. Voidaan käyttää musiikin soittamiseen.
        /// </summary>
        public MediaPlayer MediaPlayer { get; private set; }

        /// <summary>
        /// Pelin kaikkien ääniefektien voimakkuuskerroin, Väliltä 0-1.0.
        /// Tämä on sama kuin SoundEffect.MasterVolume.
        /// </summary>
        public static double MasterVolume
        {
            get => SoundEffect.MasterVolume;
            set => SoundEffect.MasterVolume = value;
        }

        /// <summary>
        /// Kirjaston mukana tuleva sisältö.
        /// Voidaan käyttää esimerkiksi sisäisten tekstuurien lataamiseen.
        /// </summary>
        public static JypeliContentManager ResourceContent { get; private set; }

        private void InitContent()
        {
            ResourceContent = new JypeliContentManager();
            MediaPlayer = new MediaPlayer();
        }

        /// <summary>
        /// Lataa kuvan contentista.
        /// </summary>
        /// <param name="name">Kuvan nimi, jos tiedostopäätettä ei anneta, etsitään tiedostoa yleisimmillä päätteillä.</param>
        /// <returns>Kuva</returns>
        public static Image LoadImage(string name)
        {
            return new Image(Device.StreamContent(name, Image.ImageExtensions));
        }

        /// <summary>
        /// Lataa kuvan Jypelin sisäisistä resursseista.
        /// </summary>
        /// <param name="name">Kuvan nimi</param>
        /// <returns>Kuva</returns>
        public static Image LoadImageFromResources(string name)
        {
            return ResourceContent.LoadInternalImage(name);
        }

        /// <summary>
        /// Lataa taulukon kuvia contentista.
        /// </summary>
        /// <param name="names">Kuvien nimet</param>
        /// <returns>Kuvataulukko</returns>
        public static Image[] LoadImages(params string[] names)
        {
            Image[] result = new Image[names.Length];
            for (int i = 0; i < names.Length; i++)
                result[i] = LoadImage(names[i]);
            return result;
        }

        /// <summary>
        /// Lataa taulukon kuvia contentista.
        /// </summary>
        /// <param name="baseName">Ennen numeroa tuleva osa nimestä.</param>
        /// <param name="startIndex">Ensimmäisen kuvan numero.</param>
        /// <param name="endIndex">Viimeisen kuvan numero.</param>
        /// <param name="zeroPad">Onko numeron edessä täytenollia.</param>
        /// <returns>Kuvataulukko</returns>
        public static Image[] LoadImages(string baseName, int startIndex, int endIndex, bool zeroPad = false)
        {
            if (startIndex > endIndex) throw new ArgumentException("Starting index must be smaller than ending index.");

            Image[] result = new Image[endIndex - startIndex];
            string format;

            if (zeroPad)
            {
                int digits = endIndex.ToString().Length;
                format = "{0}{1:" + "0".Repeat(digits) + "}";
            }
            else
            {
                format = "{0}{1}";
            }

            for (int i = startIndex; i < endIndex; i++)
            {
                string imgName = String.Format(format, baseName, i);
                result[i - startIndex] = LoadImage(imgName);
            }

            return result;
        }

        /// <summary>
        /// Soittaa ääniefektin.
        /// </summary>
        /// <param name="name">Äänen nimi</param>
        public static void PlaySound(string name)
        {
            LoadSoundEffect(name).Play();
        }

        /// <summary>
        /// Lataa ääniefektin contentista.
        /// </summary>
        /// <param name="name">Äänen nimi</param>
        /// <returns>SoundEffect-olio</returns>
        public static SoundEffect LoadSoundEffect(string name)
        {
            return new SoundEffect(name, Device.StreamContent(name, SoundEffect.SoundExtensions));
        }

        /// <summary>
        /// Lataa ääniefektin Jypelin sisäisistä resursseista.
        /// </summary>
        /// <param name="name">Äänen nimi</param>
        /// <returns>SoundEffect-olio</returns>
        public static SoundEffect LoadSoundEffectFromResources(string name)
        {
            return ResourceContent.LoadInternalSoundEffect(name);
        }

        /// <summary>
        /// Lataa taulukon ääniefektejä contentista.
        /// </summary>
        /// <param name="names">Äänien nimet</param>
        /// <returns>Taulukko SoundEffect-olioita</returns>
        public static SoundEffect[] LoadSoundEffects(params string[] names)
        {
            SoundEffect[] result = new SoundEffect[names.Length];
            for (int i = 0; i < names.Length; i++)
                result[i] = LoadSoundEffect(names[i]);
            return result;
        }

        /// <summary>
        /// Lataa fontin. Fontin tulee olla lisätty content-hakemistoon.
        /// </summary>
        /// <param name="name">Fontin tiedoston nimi.</param>
        public static Font LoadFont(string name)
        {
            return new Font(name);
        }

        /// <summary>
        /// Etsii millä päätteellä annettu tiedosto löytyy
        /// </summary>
        /// <param name="file">Tiedoston nimi</param>
        /// <param name="extensions">Päätteet joilla etsitään</param>
        /// <returns>Tiedoston nimi + pääte</returns>
        internal static string FileExtensionCheck(string file, string[] extensions)
        {
            if (!File.Exists(file))
            {
                foreach (string ex in extensions)
                {
                    if (File.Exists(file + ex))
                    {
                        file += ex;
                        break;
                    }
                }
            }
            return file;
        }
    }
}
