﻿using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JamBit
{
    class RecentSong
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }

        public int SongID { get; set; }

        public RecentSong() { }

        public RecentSong(int songID) { SongID = songID; }
    }
}