using System;
using System.Collections.Generic;
using System.Linq;

namespace tldr_sharp
{
    internal class PageComparer : IComparer<Page>
    {
        private readonly string[] _languages;
        private readonly string[] _platforms;

        public PageComparer(IEnumerable<string> platforms, IEnumerable<string> languages)
        {
            _languages = languages.ToArray();
            _platforms = platforms.ToArray();
        }

        public int Compare(Page x, Page y)
        {
            int xIndex = Array.IndexOf(_languages, x.Language);
            int yIndex = Array.IndexOf(_languages, y.Language);

            if (xIndex == yIndex) {
                xIndex = Array.IndexOf(_platforms, x.Platform);
                yIndex = Array.IndexOf(_platforms, y.Platform);
            }

            if (xIndex == yIndex) return 0;
            if (xIndex == -1) return 1;
            if (yIndex == -1) return -1;
            return xIndex - yIndex;
        }
    }
}