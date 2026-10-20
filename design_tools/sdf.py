import numpy as np, math
from PIL import Image
import sys
sys.path.insert(0,'/home/claude')

def rot(rx=0,ry=0,rz=0):
    rx,ry,rz=[math.radians(a) for a in (rx,ry,rz)]
    cx,sx,cy,sy,cz,sz=math.cos(rx),math.sin(rx),math.cos(ry),math.sin(ry),math.cos(rz),math.sin(rz)
    Rx=np.array([[1,0,0],[0,cx,-sx],[0,sx,cx]]);Ry=np.array([[cy,0,sy],[0,1,0],[-sy,0,cy]]);Rz=np.array([[cz,-sz,0],[sz,cz,0],[0,0,1]])
    return Ry@Rx@Rz
M=np.diag([-1,1,1.0])

class Part:
    def __init__(s,t,col,fur=1,k=0.1,**kw):
        s.t=t; s.col=np.array(col,float); s.fur=fur; s.k=k; s.kw=kw
def E(c,r,col,fur=1,k=0.1,rv=(0,0,0)):
    return Part('E',col,fur,k,c=np.array(c,float),r=np.array(r,float),R=rot(*rv))
def C(a,b,ra,rb,col,fur=1,k=0.1):
    return Part('C',col,fur,k,a=np.array(a,float),b=np.array(b,float),ra=ra,rb=rb)
def T(c,Rmaj,rmin,col,fur=0,k=0.02,rv=(0,0,0)):
    return Part('T',col,fur,k,c=np.array(c,float),Rm=Rmaj,rm=rmin,R=rot(*rv))
def mirror(p):
    import copy
    q=copy.deepcopy(p)
    for key in ('c','a','b'):
        if key in q.kw: q.kw[key]=M@q.kw[key]
    if 'R' in q.kw: q.kw['R']=M@q.kw['R']@M
    return q
def sym(p):
    return [p,mirror(p)]

def dist_part(p,q):
    kw=q.kw
    if q.t=='E':
        d=(p-kw['c'])@kw['R']   # R^T applied: row-vector p @ R == R^T p
        r=kw['r']
        k0=np.linalg.norm(d/r,axis=-1); k1=np.linalg.norm(d/(r*r),axis=-1)
        return k0*(k0-1)/np.maximum(k1,1e-6)
    if q.t=='C':
        a,b=kw['a'],kw['b']; ba=b-a; pa=p-a
        h=np.clip((pa@ba)/(ba@ba),0,1)
        pt=a+h[...,None]*ba
        return np.linalg.norm(p-pt,axis=-1)-(kw['ra']+(kw['rb']-kw['ra'])*h)
    if q.t=='T':
        d=(p-kw['c'])@kw['R']
        qx=np.sqrt(d[...,0]**2+d[...,2]**2)-kw['Rm']
        return np.sqrt(qx**2+d[...,1]**2)-kw['rm']

def smin(a,b,k):
    if k<=1e-5: return np.minimum(a,b)
    h=np.clip(0.5+0.5*(b-a)/k,0,1)
    return b*(1-h)+a*h-k*h*(1-h)

def sdf(p,parts):
    d=None
    for q in parts:
        di=dist_part(p,q)
        d=di if d is None else smin(d,di,q.k)
    return d

def color_at(p,parts,paints=()):
    num=0;den=0
    for q in parts:
        di=dist_part(p,q)
        w=np.exp(-np.maximum(di,0)*45)+1e-6
        num=num+w[...,None]*np.append(q.col,q.fur)
        den=den+w
    c=num/den[...,None]
    for (pc,pr,soft,col,fur) in paints:
        dd=np.linalg.norm(p-np.array(pc),axis=-1)
        t=1-np.clip((dd-(pr-soft))/soft,0,1); t=t*t*(3-2*t)
        c=c*(1-t[...,None])+np.append(col,fur)*t[...,None]
    return c

def render(parts,paints,view_dir,size=(300,260),scale=2.9,center=(0,0.4,0.3),pattern=None,pscale=2.0):
    W,H=size
    d=np.array(view_dir,float);d/=np.linalg.norm(d)
    up0=np.array([0,1,0.]);r=np.cross(up0,d);r/=np.linalg.norm(r);u=np.cross(d,r)
    xs=(np.arange(W)-W/2)/W*scale; ys=-(np.arange(H)-H/2)/W*scale
    X,Y=np.meshgrid(xs,ys)
    o=np.array(center)+X[...,None]*r+Y[...,None]*u-d*4
    P=o.reshape(-1,3); t=np.zeros(len(P)); alive=np.ones(len(P),bool); hit=np.zeros(len(P),bool)
    for i in range(90):
        idx=np.where(alive)[0]
        if len(idx)==0:break
        pp=P[idx]+d*t[idx,None]
        dd=sdf(pp,parts)
        h=dd<0.004
        hit[idx[h]]=True; alive[idx[h]]=False
        t[idx]+=dd*0.8
        alive[idx[t[idx]>8]]=False
    img=np.ones((len(P),3))*np.array([0.62,0.80,0.95])
    idx=np.where(hit)[0]
    pp=P[idx]+d*t[idx,None]
    e=0.004
    n=np.zeros_like(pp)
    for sgn in [(1,-1,-1),(-1,-1,1),(-1,1,-1),(1,1,1)]:
        sg=np.array(sgn,float); n+=sg*sdf(pp+sg*e,parts)[...,None]
    n/=np.linalg.norm(n,axis=-1,keepdims=True)
    c=color_at(pp,parts,paints)
    alb=c[:,:3]
    if pattern is not None:
        w=np.abs(n)**4; w/=w.sum(-1,keepdims=True)
        q=pp/pscale
        pat=pattern(q[:,2],q[:,1])[:,None]*w[:,0:1]+pattern(q[:,0],q[:,2])[:,None]*w[:,1:2]+pattern(q[:,0],q[:,1])[:,None]*w[:,2:3]
        alb=alb*(1+(pat-1)*c[:,3:4])
    # AO
    ao=np.ones(len(pp))
    occ=0
    for hh in (0.03,0.07,0.13,0.22):
        occ+= (hh-sdf(pp+n*hh,parts))/hh
    ao=np.clip(1-0.18*occ,0.35,1)
    L=np.array([0.45,0.8,-0.35]);L/=np.linalg.norm(L)
    dif=np.clip(n@L,0,1)
    sky=0.5+0.5*n[:,1]
    lit=(0.35+0.25*sky)*ao+0.75*dif*(0.4+0.6*ao)
    rim=(1-np.clip(-(n@d),0,1))**3*0.12
    col=alb*lit[:,None]+rim[:,None]*np.array([1,.95,.85])
    img[idx]=np.clip(col,0,1)
    return (img.reshape(H,W,3)*255).astype(np.uint8)
