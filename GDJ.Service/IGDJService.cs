using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDJ.Service
{
    public interface IGDJService
    {
        public void UpdatePlaylists(List<PlaylistMix> playlists);
    }
}
