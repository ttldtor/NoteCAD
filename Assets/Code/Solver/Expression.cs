﻿using System;
using UnityEngine;

public class Param {
	public string name;
	private double v;
	public bool changed;

	public double value {
		get { return v; }
		set {
			if(v == value) return;
			changed = true;
			v = value;
		}
	}

	public Exp exp { get; private set; }

	public Param(string name) {
		this.name = name;
		exp = new Exp(this);
	}

	public Param(string name, double value) {
		this.name = name;
		this.value = value;
		exp = new Exp(this);
	}

}

public class Exp {

	public enum Op {
		Const,
		Param,
		Add,
		Sub,
		Mul,
		Div,
		Sin,
		Cos,
		ACos,
		ASin,
		Sqrt,
		Sqr,
		Atan2,
		Abs,
		Sign,
		Neg,
		Drag,
		//Pow,
	}

	public static readonly Exp zero = new Exp(0.0);
	public static readonly Exp one  = new Exp(1.0);
	public static readonly Exp mOne = new Exp(-1.0);
	public static readonly Exp two  = new Exp(2.0);

	Op op;

	Exp a;
	Exp b;
	Param param;
	double value;

	Exp() { }

	public Exp(double value) {
		this.value = value;
		this.op = Op.Const;
	}

	internal Exp(Param p) {
		this.param = p;
		this.op = Op.Param;
	}

	public static implicit operator Exp(Param param) {
		return param.exp;
	}

	public static implicit operator Exp(double value) {
		if(value == 0.0) return zero;
		if(value == 1.0) return one;
		Exp result = new Exp();
		result.value = value;
		result.op = Op.Const;
		return result;
	}

	Exp(Op op, Exp a, Exp b) {
		this.a = a;
		this.b = b;
		this.op = op;
	}

	static public Exp operator+(Exp a, Exp b) {
		if(a.IsZeroConst()) return b;
		if(b.IsZeroConst()) return a;
		if(b.op == Op.Neg) return a - b.a;
		return new Exp(Op.Add, a, b);
	}

	static public Exp operator-(Exp a, Exp b) {
		if(a.IsZeroConst()) return -b;
		if(b.IsZeroConst()) return a;
		return new Exp(Op.Sub, a, b);
	}

	static public Exp operator*(Exp a, Exp b) {
		if(a.IsZeroConst()) return zero;
		if(b.IsZeroConst()) return zero;
		if(a.IsOneConst()) return b;
		if(b.IsOneConst()) return a;
		if(a.IsMinusOneConst()) return -b;
		if(b.IsMinusOneConst()) return -a;
		if(a.IsConst() && b.IsConst()) return a.value * b.value;
		return new Exp(Op.Mul, a, b);
	}

	static public Exp operator/(Exp a, Exp b) {
		if(b.IsOneConst()) return a;
		if(a.IsZeroConst()) return zero;
		if(b.IsMinusOneConst()) return -a;
		return new Exp(Op.Div, a, b);
	}
	//static public Exp operator^(Exp a, Exp b) { return new Exp(Op.Pow, a, b); }

	static public Exp operator-(Exp a) {
		if(a.IsZeroConst()) return a;
		if(a.IsConst()) return -a.value;
		if(a.op == Op.Neg) return a.a;
		return new Exp(Op.Neg, a, null);
	}

	static public Exp Sin  (Exp x) { return new Exp(Op.Sin,   x, null); }
	static public Exp Cos  (Exp x) { return new Exp(Op.Cos,   x, null); }
	static public Exp ACos (Exp x) { return new Exp(Op.ACos,  x, null); }
	static public Exp ASin (Exp x) { return new Exp(Op.ASin,  x, null); }
	static public Exp Sqrt (Exp x) { return new Exp(Op.Sqrt,  x, null); }
	static public Exp Sqr  (Exp x) { return new Exp(Op.Sqr,   x, null); }
	static public Exp Abs  (Exp x) { return new Exp(Op.Abs,   x, null); }
	static public Exp Sign (Exp x) { return new Exp(Op.Sign,  x, null); }
	static public Exp Atan2(Exp x, Exp y) { return new Exp(Op.Atan2, x, y); }
	//static public Exp Pow  (Exp x, Exp y) { return new Exp(Op.Pow,   x, y); }

	public Exp Drag(Exp to) {
		return new Exp(Op.Drag, this, to);
	}

	public double Eval() {
		switch(op) {
			case Op.Const:	return value;
			case Op.Param:	return param.value;
			case Op.Add:	return a.Eval() + b.Eval();
			case Op.Drag:
			case Op.Sub:	return a.Eval() - b.Eval();
			case Op.Mul:	return a.Eval() * b.Eval();
			case Op.Div: {
					var bv = b.Eval();
					if(Math.Abs(bv) < 1e-10) {
						Debug.Log("Division by zero");
						bv = 1.0;
					}
					return a.Eval() / bv;
			}
			case Op.Sin:	return Math.Sin(a.Eval());
			case Op.Cos:	return Math.Cos(a.Eval());
			case Op.ACos:	return Math.Acos(a.Eval());
			case Op.ASin:	return Math.Asin(a.Eval());
			case Op.Sqrt:	return Math.Sqrt(a.Eval());
			case Op.Sqr:	{  double av = a.Eval(); return av * av; }
			case Op.Atan2:	return Math.Atan2(a.Eval(), b.Eval());
			case Op.Abs:	return Math.Abs(a.Eval());
			case Op.Sign:	return Math.Sign(a.Eval());
			case Op.Neg:	return -a.Eval();
			//case Op.Pow:	return Math.Pow(a.Eval(), b.Eval());
		}
		return 0.0;
	}

	public bool IsZeroConst()		{ return op == Op.Const && value ==  0.0; }
	public bool IsOneConst()		{ return op == Op.Const && value ==  1.0; }
	public bool IsMinusOneConst()	{ return op == Op.Const && value == -1.0; }
	public bool IsConst()			{ return op == Op.Const; }
	public bool IsDrag()			{ return op == Op.Drag; }

	public bool IsUnary() {
		switch(op) {
			case Op.Const:
			case Op.Param:
			case Op.Sin:
			case Op.Cos:
			case Op.ACos:
			case Op.ASin:
			case Op.Sqrt:
			case Op.Sqr:
			case Op.Abs:
			case Op.Sign:
			case Op.Neg:
				return true;
		}
		return false;
	}

	public bool IsAdditive() {
		switch(op) {
			case Op.Drag:
			case Op.Sub:
			case Op.Add:
				return true;
		}
		return false;
	}

	string Quoted() {
		if(IsUnary()) return ToString();
		return "(" + ToString() + ")";
	}

	string QuotedAdd() {
		if(!IsAdditive()) return ToString();
		return "(" + ToString() + ")";
	}

	public override string ToString() {
		switch(op) {
			case Op.Const:	return value.ToStr();
			case Op.Param:	return param.name;
			case Op.Add:	return a.ToString() + " + " + b.ToString();
			case Op.Sub:	return a.ToString() + " - " + b.QuotedAdd();
			case Op.Mul:	return a.QuotedAdd() + " * " + b.QuotedAdd();
			case Op.Div:	return a.QuotedAdd() + " / " + b.Quoted();
			case Op.Sin:	return "sin(" + a.ToString() + ")";
			case Op.Cos:	return "cos(" + a.ToString() + ")";
			case Op.ASin:	return "asin(" + a.ToString() + ")";
			case Op.ACos:	return "acos(" + a.ToString() + ")";
			case Op.Sqrt:	return "sqrt(" + a.ToString() + ")";
			case Op.Sqr:	return a.Quoted() + " ^ 2";
			case Op.Abs:	return "abs(" + a.ToString() + ")";
			case Op.Sign:	return "sign(" + a.ToString() + ")";
			case Op.Atan2:	return "atan2(" + a.ToString() + ", " + b.ToString() + ")";
			case Op.Neg:	return "-" + a.Quoted();
			case Op.Drag:   return a.ToString() + " ≈ " + b.QuotedAdd();
			//case Op.Pow:	return Quoted(a) + " ^ " + Quoted(b);
		}
		return "";
	}

	public bool IsDependOn(Param p) {
		if(op == Op.Param) return param == p;
		if(a != null) {
			if(b != null) {
				return a.IsDependOn(p) || b.IsDependOn(p);
			}
			return a.IsDependOn(p);
		}
		return false;
	}

	public Exp Deriv(Param p) {
		return d(p);
	}

	Exp d(Param p) {
		switch(op) {
			case Op.Const:	return zero;
			case Op.Param:	return (param == p) ? one : zero;
			case Op.Add:	return a.d(p) + b.d(p);
			case Op.Drag:
			case Op.Sub:	return a.d(p) - b.d(p);
			case Op.Mul:	return a.d(p) * b + a * b.d(p);
			case Op.Div:	return (a.d(p) * b - a * b.d(p)) / Sqr(b);
			case Op.Sin:	return a.d(p) * Cos(a);
			case Op.Cos:	return a.d(p) * -Sin(a);
			case Op.ASin:	return a.d(p) / Sqrt(one - Sqr(a));
			case Op.ACos:	return a.d(p) * mOne / Sqrt(one - Sqr(a));
			case Op.Sqrt:	return a.d(p) / (two * Sqrt(a));
			case Op.Sqr:	return a.d(p) * two * a;
			case Op.Abs:	return a.d(p) * Sign(a);
			case Op.Sign:	return zero;
			case Op.Neg:	return -a.d(p);
			case Op.Atan2:	return (b * a.d(p) - a * b.d(p)) / (Sqr(a) + Sqr(b));
		}
		return zero;
	}

	public bool IsSubstitionForm() {
		return op == Op.Sub && a.op == Op.Param && b.op == Op.Param;
	}

	public Param GetSubstitutionParamA() {
		if(!IsSubstitionForm()) return null;
		return a.param;
	}

	public Param GetSubstitutionParamB() {
		if(!IsSubstitionForm()) return null;
		return b.param;
	}

	public void Substitute(Param pa, Param pb) {
		if(a != null) {
			a.Substitute(pa, pb);
			if(b != null) {
				b.Substitute(pa, pb);
			}
		} else
		if(op == Op.Param && param == pa) {
			param = pb;
		}
	}

	public void Walk(Action<Exp> action) {
		action(this);
		if(a != null) {
			action(a);
			if(b != null) {
				action(b);
			}
		}
	}

	public Exp DeepClone() {
		Exp result = new Exp();
		result.op = op;
		result.param = param;
		result.value = value;
		if(a != null) {
			result.a = a.DeepClone();
			if(b != null) {
				result.b = b.DeepClone();
			}
		}
		return result;
	}

}