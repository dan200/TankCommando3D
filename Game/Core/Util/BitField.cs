using Dan200.Core.Main;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;

namespace Dan200.Core.Util
{
    internal unsafe struct BitField : IEnumerable<int>, IEquatable<BitField>
    {
        private const int NUM_WORDS = 1;
        private const int NUM_BITS_PER_WORD = 64;
        private const int NUM_BITS_PER_WORD_SHIFT = 6;
        private const int NUM_BITS_PER_WORD_MASK = NUM_BITS_PER_WORD - 1;
        private const int NUM_BITS = NUM_WORDS * NUM_BITS_PER_WORD;

        public static BitField Empty = new BitField();

        public struct BitEnumerator : IEnumerator<int>
        {
            private static byte[] s_lowestBitSet = new byte[256] {
                0, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
                4, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
                5, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
                4, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
                6, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
                4, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
                5, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
                4, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
                7, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
                4, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
                5, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
                4, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
                6, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
                4, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
                5, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
                4, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0,
            };

            private BitField m_bitField;
            private int m_word;
            private int m_shift;
            private int m_current;

            public int Current
            {
                get
                {
                    return m_current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public BitEnumerator(BitField field)
            {
                m_bitField = field;
                m_word = 0;
                m_shift = 0;
                m_current = -1;
            }

            public bool MoveNext()
            {
                fixed (ulong* words = m_bitField.m_words)
                {
                    do
                    {
                        while (words[m_word] != 0)
                        {
                            byte bite = (byte)(words[m_word] & 0xff);
                            if (bite != 0)
                            {
                                int bit = s_lowestBitSet[bite];
                                m_current = bit + m_shift + (m_word * NUM_BITS_PER_WORD);
                                words[m_word] &= ~(1ul << bit);
                                return true;
                            }
                            else
                            {
                                m_shift += 8;
                                words[m_word] >>= 8;
                            }
                        }
                        m_word = m_word + 1;
                        m_shift = 0;
                    }
                    while (m_word < NUM_WORDS);
                }
                return false;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
            }
        }

        internal struct ReverseBitEnumerator : IEnumerator<int>
        {
            private static byte[] s_highestBitSet = new byte[256] {
                0, 0, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
                7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
            };

            private BitField m_bitField;
            private int m_word;
            private int m_shift;
            private int m_current;

            public int Current
            {
                get
                {
                    return m_current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public ReverseBitEnumerator(BitField field)
            {
                m_bitField = field;
                m_word = NUM_WORDS - 1;
                m_shift = NUM_BITS_PER_WORD - 8;
                m_current = -1;
            }

            public bool MoveNext()
            {
                fixed (ulong* words = m_bitField.m_words)
                {
                    do
                    {
                        while (words[m_word] != 0)
                        {
                            byte bite = (byte)((words[m_word] >> m_shift) & 0xff);
                            if (bite != 0)
                            {
                                int bit = s_highestBitSet[bite];
                                m_current = bit + m_shift + (m_word * NUM_BITS_PER_WORD);
                                words[m_word] &= ~(1ul << (bit + m_shift));
                                return true;
                            }
                            else
                            {
                                m_shift -= 8;
                            }
                        }
                        m_word = m_word - 1;
                        m_shift = NUM_BITS_PER_WORD - 8;
                    }
                    while (m_word >= 0);
                }
                return false;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
            }
        }

        public struct ReverseBitField : IEnumerable<int>
        {
            private BitField m_bitField;

            public ReverseBitField(BitField field)
            {
                m_bitField = field;
            }

            public ReverseBitEnumerator GetEnumerator()
            {
                return new ReverseBitEnumerator(m_bitField);
            }

            IEnumerator<int> IEnumerable<int>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private fixed ulong m_words[NUM_WORDS];

        public bool IsEmpty
        {
            get
            {
                fixed (ulong* words = m_words)
                {
                    ulong or = 0;
                    for (int i = 0; i < NUM_WORDS; ++i)
                    {
                        or |= words[i];
                    }
                    return or == 0;
                }
            }
        }

        public ReverseBitField Reversed
        {
            get
            {
                return new ReverseBitField(this);
            }
        }

        public bool this[int bit]
        {
            get
            {
                App.Assert(bit >= 0 && bit < NUM_BITS);
                fixed (ulong* words = m_words)
                {
                    int word = bit >> NUM_BITS_PER_WORD_SHIFT;
                    ulong mask = (ulong)1 << (bit & NUM_BITS_PER_WORD_MASK);
                    return (words[word] & mask) != 0;
                }
            }
            set
            {
                App.Assert(bit >= 0 && bit < NUM_BITS);
                fixed (ulong* words = m_words)
                {
                    int word = bit >> NUM_BITS_PER_WORD_SHIFT;
                    ulong mask = (ulong)1 << (bit & NUM_BITS_PER_WORD_MASK);
                    words[word] = (words[word] & ~mask) | (value ? mask : 0ul);
                }
            }
        }

        public BitField(BitField other)
        {
            fixed (ulong* words = m_words)
            {
                for (int i = 0; i < NUM_WORDS; ++i)
                {
                    words[i] = other.m_words[i];
                }
            }
        }

        public void Clear()
        {
            fixed (ulong* words = m_words)
            {
                for (int i = 0; i < NUM_WORDS; ++i)
                {
                    words[i] = 0;
                }
            }
        }

        private static int BitCount(ulong n)
        {
            n = n - ((n >> 1) & 0x5555555555555555UL);
            n = (n & 0x3333333333333333UL) + ((n >> 2) & 0x3333333333333333UL);
            return (int)(unchecked(((n + (n >> 4)) & 0xF0F0F0F0F0F0F0FUL) * 0x101010101010101UL) >> 56);
        }

        public int Count()
        {
            var count = 0;
            fixed (ulong* words = m_words)
            {
                for (int i = 0; i < NUM_WORDS; ++i)
                {
                    count += BitCount(words[i]);
                }
            }
            return count;
        }

        public BitEnumerator GetEnumerator()
        {
            return new BitEnumerator(this);
        }

		IEnumerator<int> IEnumerable<int>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            var result = new StringBuilder();
            int lastBit = -1;
            foreach(var bit in this)
            {
                var numZeros = (bit - lastBit - 1);
                if (numZeros > 0)
                {
                    result.Append('0', numZeros);
                }
                result.Append('1');
                lastBit = bit;
            }
            return result.ToString();
        }

		public override bool Equals(object obj)
		{
			if (obj is BitField)
			{
				return Equals((BitField)obj);
			}
			return false;
		}

		public override int GetHashCode()
		{
            fixed (ulong* words = m_words)
            {
                int hash = 0;
                for (int i = 0; i < NUM_WORDS; ++i)
                {
                    hash ^= (int)(words[i]);
                    hash ^= (int)(words[i] >> 32);
                }
                return hash;
            }
		}

		public bool Equals(BitField other)
		{
            fixed (ulong* words = m_words)
            {
                ulong diff = 0;
                for (int i = 0; i < NUM_WORDS; ++i)
                {
                    diff |= words[i] ^ other.m_words[i];
                }
                return diff == 0;
            }
		}

		public static BitField operator & (BitField a, BitField b)
		{
			var result = new BitField();
            for (int i = 0; i < NUM_WORDS; ++i)
            {
                result.m_words[i] = a.m_words[i] & b.m_words[i];
            }
            return result;
		}

		public static BitField operator | (BitField a, BitField b)
		{
			var result = new BitField();
            for (int i = 0; i < NUM_WORDS; ++i)
            {
                result.m_words[i] = a.m_words[i] | b.m_words[i];
            }
            return result;
		}

		public static BitField operator ^ (BitField a, BitField b)
		{
			var result = new BitField();
            for (int i = 0; i < NUM_WORDS; ++i)
            {
                result.m_words[i] = a.m_words[i] ^ b.m_words[i];
            }
			return result;
		}

		public static bool operator == (BitField a, BitField b)
		{
			return a.Equals(b);
		}

		public static bool operator != (BitField a, BitField b)
		{
			return !a.Equals(b);
		}

		public static BitField operator ~ (BitField a)
		{
			var result = new BitField();
            for (int i = 0; i < NUM_WORDS; ++i)
            {
                result.m_words[i] = ~a.m_words[i];
            }
            return result;
		}
    }
}
