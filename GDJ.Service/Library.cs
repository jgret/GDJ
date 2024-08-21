using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDJ.Service
{
    public class Library
    {
        private Dictionary<string, Playlist> playlist; // playlist id -> playlist with tracklist
        public Library()
        {
            playlist = new Dictionary<string, Playlist>();
        }
    }
}
