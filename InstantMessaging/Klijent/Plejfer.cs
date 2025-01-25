using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Klijent
{
    public class Plejfer
    {
        private char[,] matrix = new char[5, 5];
        private Dictionary<char, (int, int)> charPositions = new Dictionary<char, (int, int)>();

        public Plejfer(string key)
        {
            GenerateMatrix(key);
        }

        // Generiše Plejfer matricu na osnovu ključne reči
        private void GenerateMatrix(string key)
        {
            key = key.ToUpper().Replace("J", "I"); // Zamenjujemo 'J' sa 'I'
            HashSet<char> seen = new HashSet<char>();

            int index = 0;
            for (int row = 0; row < 5; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    while (index < key.Length && seen.Contains(key[index]))
                    {
                        index++;
                    }

                    if (index < key.Length)
                    {
                        matrix[row, col] = key[index];
                        charPositions[key[index]] = (row, col);
                        seen.Add(key[index]);
                        index++;
                    }
                    else
                    {
                        for (char c = 'A'; c <= 'Z'; c++)
                        {
                            if (!seen.Contains(c) && c != 'J')
                            {
                                matrix[row, col] = c;
                                charPositions[c] = (row, col);
                                seen.Add(c);
                                break;
                            }
                        }
                    }
                }
            }
        }

        // Funkcija za šifrovanje poruke
        public string Encrypt(string text)
        {
            StringBuilder result = new StringBuilder();
            text = text.ToUpper().Replace("J", "I"); // Zamenjujemo 'J' sa 'I'
            if (text.Length % 2 != 0)
            {
                text += "X"; // Ako je broj karaktera neparan, dodajemo 'X'
            }

            for (int i = 0; i < text.Length; i += 2)
            {
                char firstChar = text[i];
                char secondChar = text[i + 1];

                var firstPos = charPositions[firstChar];
                var secondPos = charPositions[secondChar];

                if (firstPos.Item1 == secondPos.Item1)
                {
                    // Isto u redu, pomeramo ih u desno
                    result.Append(matrix[firstPos.Item1, (firstPos.Item2 + 1) % 5]);
                    result.Append(matrix[secondPos.Item1, (secondPos.Item2 + 1) % 5]);
                }
                else if (firstPos.Item2 == secondPos.Item2)
                {
                    // Isto u koloni, pomeramo ih naniže
                    result.Append(matrix[(firstPos.Item1 + 1) % 5, firstPos.Item2]);
                    result.Append(matrix[(secondPos.Item1 + 1) % 5, secondPos.Item2]);
                }
                else
                {
                    // Različito u redu i koloni, formiramo pravougaonik
                    result.Append(matrix[firstPos.Item1, secondPos.Item2]);
                    result.Append(matrix[secondPos.Item1, firstPos.Item2]);
                }
            }

            return result.ToString();
        }

        // Funkcija za dešifrovanje poruke
        public string Decrypt(string text)
        {
            StringBuilder result = new StringBuilder();
            text = text.ToUpper().Replace("J", "I");

            for (int i = 0; i < text.Length; i += 2)
            {
                char firstChar = text[i];
                char secondChar = text[i + 1];

                var firstPos = charPositions[firstChar];
                var secondPos = charPositions[secondChar];

                if (firstPos.Item1 == secondPos.Item1)
                {
                    // Isto u redu, pomeramo ih ulevo
                    result.Append(matrix[firstPos.Item1, (firstPos.Item2 + 4) % 5]);
                    result.Append(matrix[secondPos.Item1, (secondPos.Item2 + 4) % 5]);
                }
                else if (firstPos.Item2 == secondPos.Item2)
                {
                    // Isto u koloni, pomeramo ih nagore
                    result.Append(matrix[(firstPos.Item1 + 4) % 5, firstPos.Item2]);
                    result.Append(matrix[(secondPos.Item1 + 4) % 5, secondPos.Item2]);
                }
                else
                {
                    // Različito u redu i koloni, formiramo pravougaonik
                    result.Append(matrix[firstPos.Item1, secondPos.Item2]);
                    result.Append(matrix[secondPos.Item1, firstPos.Item2]);
                }
            }

            return result.ToString();
        }
    }

}

