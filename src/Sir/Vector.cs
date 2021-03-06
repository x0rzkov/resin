﻿using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Sir
{
    public class IndexedVector : IVector
    {
        public string Label { get; }
        public Vector<float> Value { get; private set; }
        public int ComponentCount => ((SparseVectorStorage<float>)Value.Storage).ValueCount;

        public IndexedVector(int numOfDimensions)
        {
            Value = CreateVector.Sparse(SparseVectorStorage<float>.OfEnumerable(new float[numOfDimensions]));
        }

        public IndexedVector(IEnumerable<float> values, string label = null)
        {
            Value = CreateVector.Sparse(SparseVectorStorage<float>.OfEnumerable(values));
            Label = label;
        }

        public IndexedVector(SortedList<int, float> dictionary, int numOfDimensions, string label = null)
        {
            var tuples = new Tuple<int, float>[Math.Min(dictionary.Count, numOfDimensions)];
            var i = 0;

            foreach (var p in dictionary)
            {
                if (i == numOfDimensions)
                    break;

                tuples[i++] = new Tuple<int, float>(p.Key, p.Value);
            }

            Value = CreateVector.SparseOfIndexed(numOfDimensions, tuples);
            Label = label;
        }

        public IndexedVector(int[] index, float[] values, int numOfDimensions, string label = null)
        {
            var tuples = new Tuple<int, float>[Math.Min(index.Length, numOfDimensions)];

            for (int i = 0; i < index.Length; i++)
            {
                if (i == numOfDimensions)
                    break;

                tuples[i] = new Tuple<int, float>(index[i], values[i]);
            }

            Value = CreateVector.Sparse(
                SparseVectorStorage<float>.OfIndexedEnumerable(numOfDimensions, tuples));

            Label = label;
        }

        public IndexedVector(Tuple<int, float>[] tuples, int vectorWidth, string label = null)
        {
            Value = CreateVector.SparseOfIndexed(vectorWidth, tuples);
        }

        public IndexedVector(Vector<float> vector, string label = null)
        {
            Value = vector;
            Label = label;
        }

        public void Serialize(Stream stream)
        {
            var storage = (SparseVectorStorage<float>)Value.Storage;
            var indices = MemoryMarshal.Cast<int, byte>(storage.Indices);
            var values = MemoryMarshal.Cast<float, byte>(storage.Values);

            stream.Write(indices);
            stream.Write(values);
        }

        public void Add(IVector vector)
        {
            Value = Value.Add(vector.Value);
        }

        public void Subtract(IVector vector)
        {
            Value = Value.Subtract(vector.Value);

            Value.CoerceZero(0);
        }

        public IVector Multiply(float scalar)
        {
            var newVector = Value.Multiply(scalar);
            return new IndexedVector(newVector);
        }

        public void Average(IVector vector)
        {
            Value = Value.Add(vector.Value).Divide(2);
        }

        public override string ToString()
        {
            return Label == null ? Value.ToString() : Label.ToString();
        }
    }

    public interface IVector
    {
        Vector<float> Value { get; }
        void Serialize(Stream stream);
        int ComponentCount { get; }
        string Label { get; }
        void Add(IVector vector);
        void Subtract(IVector vector);
        IVector Multiply(float scalar);
        void Average(IVector vector);
    }
}