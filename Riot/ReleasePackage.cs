using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace WintermintClient.Riot
{
    public struct ReleasePackage
    {
        public readonly int Version;

        public string String
        {
            get
            {
                object[] version = new object[] { this.Version >> 24 & 255, this.Version >> 16 & 255, this.Version >> 8 & 255, this.Version & 255 };
                return string.Format("{0}.{1}.{2}.{3}", version);
            }
        }

        public ReleasePackage(int version)
        {
            this = new ReleasePackage()
            {
                Version = version
            };
        }

        public ReleasePackage(string s)
        {
            this = new ReleasePackage();
            string[] strArrays = s.Split(new char[] { '.' });
            if ((int)strArrays.Length != 4)
            {
                throw new ArgumentOutOfRangeException();
            }
            this.Version = strArrays.Aggregate<string, int>(0, (int v, string b) => v << 8 | byte.Parse(b));
        }

        public bool Equals(ReleasePackage other)
        {
            return this.Version == other.Version;
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(null, obj))
            {
                return false;
            }
            if (!(obj is ReleasePackage))
            {
                return false;
            }
            return this.Equals((ReleasePackage)obj);
        }

        public override int GetHashCode()
        {
            return this.Version;
        }

        public static bool IsVersionString(string s)
        {
            bool flag;
            try
            {
                ReleasePackage releasePackage = new ReleasePackage(s);
                return true;
            }
            catch (Exception exception)
            {
                flag = false;
            }
            return flag;
        }

        public override string ToString()
        {
            return this.String;
        }
    }
}