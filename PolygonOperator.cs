using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PolygonOperator : MonoBehaviour
{

    static private int indexA;
    static private int indexB;

    static private List<List<Vector2>> outputPolygons;

    static private List<Vector2> basisVerts;
    static private List<Vector2> patternVerts;

    static private List<Vertex> basis;
    static private List<Vertex> pattern;


    static Mode mode; // flase = subtract, true = add;

    static public List<List<Vector2>> cutpolygon(List<Vector2> basisInpt, List<Vector2> patternInpt)
    {
        initializeFields(basisInpt, patternInpt);

        mode = Mode.subtract;

        createVertLists();
        step1();

        return outputPolygons;
    }
    static public List<List<Vector2>> addpolygon(List<Vector2> basisInpt, List<Vector2> patternInpt)
    {
        initializeFields(basisInpt, patternInpt);

        mode = Mode.add;

        createVertLists();
        step1();

        return outputPolygons;
    }

    static void initializeFields(List<Vector2> basisInpt, List<Vector2> patternInpt)
    {
        outputPolygons = new List<List<Vector2>>();

        basisVerts = new List<Vector2>(basisInpt);
        patternVerts = new List<Vector2>(patternInpt);

    }

//--------------------------------------------------------------------------------------------------------------------
//--------- Methodes necessary for creating vertexlist----------------------------------------------------------------
//--------------------------------------------------------------------------------------------------------------------
    
    static void createVertLists()
    {
        initializeVertexList();
        intersectPolygons();
        insertPoints(basis, pattern);
        insertPoints(pattern, basis);
        setVertsCross();
        setVertsOutside();
    }
    static void initializeVertexList()
    {
        basis = new List<Vertex>();
        pattern = new List<Vertex>();

        foreach (Vector2 v2 in basisVerts)
        {
            basis.Add(new Vertex(v2));
        }

        foreach (Vector2 v2 in patternVerts)
        {
            pattern.Add(new Vertex(v2));
        }
    }
    static bool intersectPolygons()
    {
        for (int i = 0; i < basis.Count; i++)
        {
            for (int j = 0; j < pattern.Count; j++)
            {
                bool inter = Utils.getIntersectionV2Cramer(basis[i].position, Utils.getItem<Vertex>(basis, i + 1).position, pattern[j].position, Utils.getItem<Vertex>(pattern, j + 1).position, out Vector2 intersection);

                if (inter == true)
                {
                    basis.Insert(i + 1, new Vertex(intersection, true));
                    pattern.Insert(j + 1, new Vertex(intersection, true));

                    return intersectPolygons();
                }

            }
        }

        return true;

    }
    static void setVertsCross()
    {
        for (int i = 0; i < basis.Count; i++)
        {
            for (int j = 0; j < pattern.Count; j++)
            {
                if (basis[i].position == pattern[j].position)
                {
                    basis[i].cross = j;
                    pattern[j].cross = i;
                }
            }
        }
    }
    static void insertPoints(List<Vertex> thisPolygon, List<Vertex> otherPolygon)
    {
        for (int i = 0; i < thisPolygon.Count; i++)
        {
            Vertex v1 = Utils.getItem<Vertex>(thisPolygon, i);
            Vertex v2 = Utils.getItem<Vertex>(thisPolygon, i + 1);

            if (v1.intersection == true && v2.intersection == true)
            {
                Vector2 midPoint = v1.position + (v2.position - v1.position) / 2;

                if (insidePolygon(midPoint, otherPolygon) == false)
                {
                    thisPolygon.Insert(i + 1, new Vertex(midPoint));
                }
            }
        }
    }
    static void setVertsOutside()
    {
        foreach (Vertex vertex in basis)
        {
            if (vertex.intersection == false && insidePolygon(vertex.position, pattern) == false)
            {
                vertex.outside = true;
            }
        }

        foreach (Vertex vertex in pattern)
        {
            if (vertex.intersection == false && insidePolygon(vertex.position, basis) == false)
            {
                vertex.outside = true;
            }
        }
    }
    static bool insidePolygon(Vector2 aPoint, List<Vertex> bVertList)
    {
        float sum = 0;

        for (int i = 0; i < bVertList.Count; i++)
        {
            Vector2 xA;
            Vector2 xB;

            if (i + 1 != bVertList.Count)
            {
                xA = bVertList[i].position - aPoint;
                xB = bVertList[i + 1].position - aPoint;
            }
            else
            {
                xA = bVertList[i].position - aPoint;
                xB = bVertList[0].position - aPoint;
            }

            sum += Vector2.SignedAngle(xA, xB);
        }


        if (sum < -359 && sum > -361)
        {
            return true;
        }

        else
        {
            return false;
        }
    }


//--------------------------------------------------------------------------------------------------------------------
//--------- Methodes necessary for adding or subtraction polygons-----------------------------------------------------
//--------- based of https://www.pnnl.gov/main/publications/external/technical_reports/PNNL-SA-97135.pdf -------------
//--------------------------------------------------------------------------------------------------------------------
    static void step1()
    {
        if (getUnprocessedOutsideVertex() == true)
        {
            outputPolygons.Add(new List<Vector2>());
            step2();
        }

    }
    static void step2()
    {

        if (outputPolygons.Last().Count > 1 && outputPolygons.Last()[0] == basis[indexA].position)
        {
            step1();
        }

        else
        {
            outputPolygons.Last().Add(basis[indexA].position);
            basis[indexA].processed = true;

            step3();
        }

    }
    static void step3()
    {
        if (basis[indexA].cross == -1)
        {
            step9();
        }
        else
        {
            step4();
        }
    }
    static void step4()
    {
        indexB = basis[indexA].cross;
        step5();
    }
    static void step5()
    {
        if(mode == Mode.subtract)
        {
            decreaseIndex(ref indexB, pattern);
        }

        else if(mode == Mode.add)
        {
            increaseIndex(ref indexB, pattern);
        }

        step6();
    }
    static void step6()
    {
        outputPolygons.Last().Add(pattern[indexB].position);
        step7();
    }
    static void step7()
    {
        if (pattern[indexB].cross == -1)
        {
            step5();
        }
        else
        {
            step8();
        }
    }
    static void step8()
    {
        indexA = pattern[indexB].cross;
        increaseIndex(ref indexA, basis);

        step2();
    }
    static void step9()
    {
        increaseIndex(ref indexA, basis);
        step2();
    }

    static void increaseIndex(ref int index, List<Vertex> list)
    {
        if (index + 1 >= list.Count)
        {
            index = 0;
        }
        else
        {
            index++;
        }
    }
    static void decreaseIndex(ref int index, List<Vertex> list)
    {
        if (index - 1 < 0)
        {
            index = list.Count - 1;
        }

        else
        {
            index--;
        }
    }
    static bool getUnprocessedOutsideVertex()
    {
        for (int i = 0; i < basis.Count; i++)
        {
            if (basis[i].outside == true && basis[i].processed == false)
            {
                indexA = i;
                return true;
            }
        }

        indexA = -1;
        return false;
    }

    class Vertex
    {
        public Vector2 position;
        public bool outside;
        public int cross;
        public bool processed;

        public bool intersection;

        public Vertex(Vector2 pos)
        {
            this.position = pos;
            this.cross = -1;
        }

        public Vertex(Vector2 pos, bool intersection)
        {
            this.position = pos;
            this.intersection = intersection;
            this.cross = -1;
        }
    }
    enum Mode
    {
        add,
        subtract
    }
}
