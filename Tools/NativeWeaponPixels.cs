// Pixel-grid authoring with the user-requested half-resolution output.
// Crop the authored grid, then sample nearest neighbors without blending colors.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;

public static class NativeWeaponPixels
{
    // Weapon0: near-black body, midnight-blue bevel and electric-blue cutting edge.
    static readonly Color[] Colors = {
        Color.Transparent, Color.FromArgb(0,3,8), Color.FromArgb(0,9,24),
        Color.FromArgb(0,21,56), Color.FromArgb(0,41,113),
        Color.FromArgb(0,72,183), Color.FromArgb(0,108,239),
        Color.FromArgb(21,137,255), Color.FromArgb(112,0,93,255),
        Color.FromArgb(48,0,93,255)
    };
    sealed class Grid {
        public int W,H; public byte[,] P;
        public Grid(int w,int h){W=w;H=h;P=new byte[w,h];}
        public void Pixel(int x,int y,int c){if(x>=0&&x<W&&y>=0&&y<H)P[x,y]=(byte)c;}
        public void Rect(int x,int y,int w,int h,int c){for(int yy=y;yy<y+h;yy++)for(int xx=x;xx<x+w;xx++)Pixel(xx,yy,c);}
        public void Poly(int c,params int[] xy){
            // Evaluate coverage on the native pixel grid; never render to a larger canvas.
            for(int y=0;y<H;y++)for(int x=0;x<W;x++){
                bool inside=false;int n=xy.Length/2;
                for(int i=0,j=n-1;i<n;j=i++){
                    double xi=xy[2*i],yi=xy[2*i+1],xj=xy[2*j],yj=xy[2*j+1];
                    if((yi>y+.5)!=(yj>y+.5) && x+.5<(xj-xi)*(y+.5-yi)/(yj-yi)+xi)inside=!inside;
                }
                if(inside)Pixel(x,y,c);
            }
        }
        public Grid Crop(){
            int l=W,t=H,r=-1,b=-1;
            for(int y=0;y<H;y++)for(int x=0;x<W;x++)if(P[x,y]!=0){l=Math.Min(l,x);r=Math.Max(r,x);t=Math.Min(t,y);b=Math.Max(b,y);}
            var g=new Grid(r-l+1,b-t+1);
            for(int y=0;y<g.H;y++)for(int x=0;x<g.W;x++)g.P[x,y]=P[x+l,y+t];
            return g;
        }
        public Grid Half(){
            var g=new Grid((W+1)/2,(H+1)/2);
            for(int y=0;y<g.H;y++)for(int x=0;x<g.W;x++)
                g.P[x,y]=P[Math.Min(W-1,(int)((x+.5)*W/g.W)),Math.Min(H-1,(int)((y+.5)*H/g.H))];
            return g.Crop();
        }
        public void Save(string path){using(var b=new Bitmap(W,H,PixelFormat.Format32bppArgb)){for(int y=0;y<H;y++)for(int x=0;x<W;x++)b.SetPixel(x,y,Colors[P[x,y]]);b.Save(path,ImageFormat.Png);}}
    }
    static void Grip(Grid g,int end,int cy){
        // Weapon0 has a low, almost black grip and a small blue-lit end cap.
        g.Poly(1,0,cy-4,5,cy-9,end-11,cy-9,end,cy-5,end,cy+8,end-21,cy+8,end-24,cy+5,21,cy+5,18,cy+10,5,cy+8,0,cy+3);
        g.Rect(7,cy-7,end-17,12,2);
        g.Rect(11,cy-6,end-28,2,1);
        // Large dark grip planes; no ridges, bolts or outlined panels.
        g.Poly(1,22,cy-7,30,cy-7,38,cy+5,30,cy+5);
        g.Poly(1,end-45,cy-7,end-36,cy-7,end-28,cy+5,end-37,cy+5);
        g.Rect(end-15,cy-7,7,13,1);g.Rect(end-7,cy-5,3,10,3);
        g.Poly(5,1,cy-4,4,cy-6,4,cy+3,9,cy+7,6,cy+8,1,cy+3);
        g.Rect(0,cy-3,2,5,7);
    }
    static void Guard(Grid g,int x,int cy){
        // Original-like asymmetric shoulder, integrated into the spine.
        g.Poly(1,x-5,cy-9,x-1,cy-16,x+11,cy-17,x+11,cy-10,x+6,cy-5,x+6,cy+13,x+2,cy+18,x-8,cy+18,x-8,cy+6);
        g.Rect(x-4,cy-8,7,20,2);g.Rect(x+3,cy-8,3,15,3);
    }
    static void BladeColumn(Grid g,int x,int top,int bottom,bool dark){
        if(bottom<top)bottom=top;
        int h=bottom-top+1;g.Rect(x,top,1,h,1);
        if(h<3)return;
        g.Rect(x,top+1,1,h-2,2);
        if(h>6)g.Rect(x,top+Math.Max(2,h/3),1,Math.Max(1,h/3),3);
        int edge=Math.Max(1,h/4);
        g.Rect(x,bottom-edge,1,edge,4);
        g.Rect(x,bottom-Math.Max(1,edge/2),1,Math.Max(1,edge/2),dark?5:6);
        g.Pixel(x,bottom,7);
        // Pixel-authored blue falloff: explicit alpha, no filtering or blur pass.
        g.Pixel(x,bottom+1,8);g.Pixel(x,bottom+2,9);
    }
    static Grid Sword(int length,int start,int cy,int depth,bool heavy,bool dagger){
        var g=new Grid(length,cy+depth+32);Grip(g,start,cy);Guard(g,start,cy);
        for(int x=start+4;x<length;x++){
            int left=length-1-x;
            int taper=dagger?length/3:heavy?length/5:length/6;
            double k=Math.Min(1,(double)left/taper);
            int top=cy-(int)Math.Round((depth/2)*k);
            int bottom=cy+(int)Math.Round(depth*k);
            // The heavy blade has a clipped spine, avoiding a medieval spearhead silhouette.
            if(heavy && left<taper)top=cy-depth/2+(int)Math.Round((depth/2.0)*Math.Pow(1-k,3));
            BladeColumn(g,x,top,bottom,heavy);
        }
        // A single unbordered dark bevel joins the black hilt to the blade.
        g.Poly(3,start+5,cy-depth/2+1,start+18,cy-depth/2+1,start+8,cy+depth-3,start+5,cy+depth-3);
        return g.Crop();
    }
    static Grid Katana(int length,int start,bool lower){
        var g=new Grid(length,96);int cy=49;
        Grip(g,start,cy);Guard(g,start,cy);
        for(int x=start+4;x<length;x++){
            double p=(double)(x-start-4)/(length-start-5);
            int top=cy-8-(int)Math.Round(24*p*p*p);
            int bottom=cy+12-(int)Math.Round(44*p*p*p);
            BladeColumn(g,x,top,bottom,lower);
        }
        return g.Crop();
    }
    static Grid Gun(){
        var g=new Grid(216,104);
        g.Poly(1,25,10,43,2,72,2,79,8,192,8,206,16,216,16,216,36,187,40,103,42,94,58,52,58,36,103,1,103,17,53,19,33,9,28,9,20);
        g.Poly(2,24,18,46,9,71,9,78,14,190,14,202,19,210,19,210,29,185,34,75,35,46,42,25,31);
        g.Poly(3,78,17,187,17,199,21,185,26,76,27,62,21);
        g.Poly(4,79,29,195,27,209,23,209,28,192,34,78,35,48,42,42,38);
        g.Poly(6,80,33,193,30,211,25,211,29,193,35,79,37,49,44,46,42);
        g.Rect(213,19,2,11,7);g.Rect(215,19,1,12,8);
        g.Poly(2,26,46,53,46,35,98,7,98);g.Poly(1,30,48,42,48,24,95,13,95);
        g.Poly(3,9,97,33,97,32,101,7,101);g.Rect(7,100,22,1,5);
        g.Poly(0,57,44,88,44,79,53,54,53);g.Rect(77,44,3,7,3);
        g.Rect(28,17,11,2,3);g.Rect(35,17,4,2,5);
        return g.Crop();
    }
    public static string Author(string output){
        Directory.CreateDirectory(output);
        Grid a=Katana(336,92,false),b=Katana(336,92,true);
        var dual=new Grid(a.W,a.H+b.H);
        for(int y=0;y<a.H;y++)for(int x=0;x<a.W;x++)dual.P[x,y]=a.P[x,y];
        for(int y=0;y<b.H;y++)for(int x=0;x<b.W;x++)dual.P[x,y+a.H]=b.P[x,y];
        var entries=new Dictionary<string,Grid>{
            {"Weapon_1001_Sword",Sword(384,104,35,17,false,false)},
            {"Weapon_2001_Greatsword",Sword(512,135,43,31,true,false)},
            {"Weapon_3001_Dagger",Sword(224,65,31,16,false,true)},
            {"Weapon_4001_Katana",Katana(496,140,false)},
            {"Weapon_5001_DualBlades",dual},{"Weapon_6001_Gun",Gun()}
        };
        string report="";
        foreach(var kv in entries){var half=kv.Value.Half();half.Save(Path.Combine(output,kv.Key+".png"));report+=kv.Key+" "+half.W+"x"+half.H+"\n";}
        return report+"Dual blade height: "+((a.H+1)/2);
    }
}
