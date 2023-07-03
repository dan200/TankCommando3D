// This file is provided under The MIT License as part of Steamworks.NET.
// Copyright (c) 2013-2015 Riley Labrecque
// Please see the included LICENSE.txt for additional information.

// Changes to this file will be reverted when you update Steamworks.NET

namespace Steamworks
{
    public struct SteamItemInstanceID_t : System.IEquatable<SteamItemInstanceID_t>, System.IComparable<SteamItemInstanceID_t>
    {
        public static readonly SteamItemInstanceID_t Invalid = new SteamItemInstanceID_t(0xFFFFFFFFFFFFFFFF);
        public ulong m_SteamItemInstanceID;

        public SteamItemInstanceID_t(ulong value)
        {
            m_SteamItemInstanceID = value;
        }

        public override string ToString()
        {
            return m_SteamItemInstanceID.ToString();
        }

        public override bool Equals(object other)
        {
            return other is SteamItemInstanceID_t && this == (SteamItemInstanceID_t)other;
        }

        public override int GetHashCode()
        {
            return m_SteamItemInstanceID.GetHashCode();
        }

        public static bool operator ==(SteamItemInstanceID_t x, SteamItemInstanceID_t y)
        {
            return x.m_SteamItemInstanceID == y.m_SteamItemInstanceID;
        }

        public static bool operator !=(SteamItemInstanceID_t x, SteamItemInstanceID_t y)
        {
            return !(x == y);
        }

        public static explicit operator SteamItemInstanceID_t(ulong value)
        {
            return new SteamItemInstanceID_t(value);
        }

        public static explicit operator ulong(SteamItemInstanceID_t that)
        {
            return that.m_SteamItemInstanceID;
        }

        public bool Equals(SteamItemInstanceID_t other)
        {
            return m_SteamItemInstanceID == other.m_SteamItemInstanceID;
        }

        public int CompareTo(SteamItemInstanceID_t other)
        {
            return m_SteamItemInstanceID.CompareTo(other.m_SteamItemInstanceID);
        }
    }
}
