using System.Windows;
namespace AionMeter.App.Services;

internal static class PreviewPacking
{
    internal static Dictionary<string,Rect>? Pack(Rect area,IReadOnlyList<(string Id,Size Size)> items,IEnumerable<Rect> reserved)
    {
        var free=new List<Rect>{area};
        void Cut(Rect used)
        {
            var next=new List<Rect>();
            foreach(var r in free)
            {
                if(!r.IntersectsWith(used)){next.Add(r);continue;}
                if(used.Left>r.Left)next.Add(new(r.Left,r.Top,used.Left-r.Left,r.Height));
                if(used.Right<r.Right)next.Add(new(used.Right,r.Top,r.Right-used.Right,r.Height));
                if(used.Top>r.Top)next.Add(new(r.Left,r.Top,r.Width,used.Top-r.Top));
                if(used.Bottom<r.Bottom)next.Add(new(r.Left,used.Bottom,r.Width,r.Bottom-used.Bottom));
            }
            free=next.Where((r,i)=>r.Width>0 && r.Height>0 && !next.Where((_,j)=>j!=i).Any(other=>other.Contains(r) && (other!=r || next.IndexOf(other)<i))).ToList();
        }
        foreach(var r in reserved)Cut(r);
        var placed=new Dictionary<string,Rect>();
        foreach(var item in items.OrderByDescending(i=>i.Id=="dps").ThenByDescending(i=>i.Size.Width*i.Size.Height))
        {
            var candidates=free.Where(r=>r.Width>=item.Size.Width && r.Height>=item.Size.Height)
                .OrderBy(r=>r.Width*r.Height).ThenBy(r=>r.Top).ThenBy(r=>r.Left).ToArray();
            if(candidates.Length==0)return null;
            var slot=candidates[0];var bounds=new Rect(slot.TopLeft,item.Size);
            placed[item.Id]=bounds;var padded=bounds;padded.Inflate(5,5);Cut(padded);
        }
        return placed;
    }
}
