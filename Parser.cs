
using System;
using System.IO;
using System.Text;

namespace Bool {

public class Parser {
	public const int _EOF = 0;
	public const int _ident = 1;
	public const int _EOL = 2;
	// terminals
	public const int EOF_SYM = 0;
	public const int ident_Sym = 1;
	public const int EOL_Sym = 2;
	public const int equal_Sym = 3;
	public const int semicolon_Sym = 4;
	public const int barbar_Sym = 5;
	public const int or_Sym = 6;
	public const int lparen_Sym = 7;
	public const int rparen_Sym = 8;
	public const int andand_Sym = 9;
	public const int and_Sym = 10;
	public const int not_Sym = 11;
	public const int bang_Sym = 12;
	public const int NOT_SYM = 13;
	// pragmas

	public const int maxT = 13;

	const bool T = true;
	const bool x = false;
	const int minErrDist = 2;

	public static Token token;    // last recognized token   /* pdt */
	public static Token la;       // lookahead token
	static int errDist = minErrDist;

	

	static void SynErr (int n) {
		if (errDist >= minErrDist) Errors.SynErr(la.line, la.col, n);
		errDist = 0;
	}

	public static void SemErr (string msg) {
		if (errDist >= minErrDist) Errors.Error(token.line, token.col, msg); /* pdt */
		errDist = 0;
	}

	public static void SemError (string msg) {
		if (errDist >= minErrDist) Errors.Error(token.line, token.col, msg); /* pdt */
		errDist = 0;
	}

	public static void Warning (string msg) { /* pdt */
		if (errDist >= minErrDist) Errors.Warn(token.line, token.col, msg);
		errDist = 2; //++ 2009/11/04
	}

	public static bool Successful() { /* pdt */
		return Errors.count == 0;
	}

	public static string LexString() { /* pdt */
		return token.val;
	}

	public static string LookAheadString() { /* pdt */
		return la.val;
	}

	static void Get () {
		for (;;) {
			token = la; /* pdt */
			la = Scanner.Scan();
			if (la.kind <= maxT) { ++errDist; break; }

			la = token; /* pdt */
		}
	}

	static void Expect (int n) {
		if (la.kind==n) Get(); else { SynErr(n); }
	}

	static bool StartOf (int s) {
		return set[s, la.kind];
	}

	static void ExpectWeak (int n, int follow) {
		if (la.kind == n) Get();
		else {
			SynErr(n);
			while (!StartOf(follow)) Get();
		}
	}

	static bool WeakSeparator (int n, int syFol, int repFol) {
		bool[] s = new bool[maxT+1];
		if (la.kind == n) { Get(); return true; }
		else if (StartOf(repFol)) return false;
		else {
			for (int i=0; i <= maxT; i++) {
				s[i] = set[syFol, i] || set[repFol, i] || set[0, i];
			}
			SynErr(n);
			while (!s[la.kind]) Get();
			return StartOf(syFol);
		}
	}

	static void Bool() {
		Expect(ident_Sym);
		Expect(equal_Sym);
		booleanExpr();
		Expect(semicolon_Sym);
	}

	static void booleanExpr() {
		if (la.kind == ident_Sym) {
			Get();
			while (la.kind == barbar_Sym) {
				Get();
				andExpr();
			}
		} else if (la.kind == ident_Sym) {
			Get();
			while (la.kind == or_Sym) {
				Get();
				andExpr();
			}
		} else if (la.kind == lparen_Sym) {
			Get();
			booleanExpr();
			Expect(rparen_Sym);
		} else SynErr(14);
	}

	static void andExpr() {
		if (la.kind == ident_Sym) {
			Get();
			while (la.kind == andand_Sym) {
				Get();
				notExpr();
			}
		} else if (la.kind == ident_Sym) {
			Get();
			while (la.kind == and_Sym) {
				Get();
				notExpr();
			}
		} else SynErr(15);
	}

	static void notExpr() {
		if (la.kind == ident_Sym) {
			Get();
		} else if (la.kind == not_Sym) {
			Get();
			notExpr();
		} else if (la.kind == bang_Sym) {
			Get();
			notExpr();
		} else if (la.kind == ident_Sym || la.kind == lparen_Sym) {
			booleanExpr();
		} else SynErr(16);
	}



	public static void Parse() {
		la = new Token();
		la.val = "";
		Get();
		Bool();
		Expect(EOF_SYM);

	}

	static bool[,] set = {
		{T,x,x,x, x,x,x,x, x,x,x,x, x,x,x}

	};

} // end Parser

/* pdt - considerable extension from here on */

public class ErrorRec {
	public int line, col, num;
	public string str;
	public ErrorRec next;

	public ErrorRec(int l, int c, string s) {
		line = l; col = c; str = s; next = null;
	}

} // end ErrorRec

public class Errors {

	public static int count = 0;                                     // number of errors detected
	public static int warns = 0;                                     // number of warnings detected
	public static string errMsgFormat = "file {0} : ({1}, {2}) {3}"; // 0=file 1=line, 2=column, 3=text
	static string fileName = "";
	static string listName = "";
	static bool mergeErrors = false;
	static StreamWriter mergedList;

	static ErrorRec first = null, last;
	static bool eof = false;

	static string GetLine() {
		char ch, CR = '\r', LF = '\n';
		int l = 0;
		StringBuilder s = new StringBuilder();
		ch = (char) Buffer.Read();
		while (ch != Buffer.EOF && ch != CR && ch != LF) {
			s.Append(ch); l++; ch = (char) Buffer.Read();
		}
		eof = (l == 0 && ch == Buffer.EOF);
		if (ch == CR) {  // check for MS-DOS
			ch = (char) Buffer.Read();
			if (ch != LF && ch != Buffer.EOF) Buffer.Pos--;
		}
		return s.ToString();
	}

	static void Display (string s, ErrorRec e) {
		mergedList.Write("**** ");
		for (int c = 1; c < e.col; c++)
			if (s[c-1] == '\t') mergedList.Write("\t"); else mergedList.Write(" ");
		mergedList.WriteLine("^ " + e.str);
	}

	public static void Init (string fn, string dir, bool merge) {
		fileName = fn;
		listName = dir + "listing.txt";
		mergeErrors = merge;
		if (mergeErrors)
			try {
				mergedList = new StreamWriter(new FileStream(listName, FileMode.Create));
			} catch (IOException) {
				Errors.Exception("-- could not open " + listName);
			}
	}

	public static void Summarize () {
		if (mergeErrors) {
			mergedList.WriteLine();
			ErrorRec cur = first;
			Buffer.Pos = 0;
			int lnr = 1;
			string s = GetLine();
			while (!eof) {
				mergedList.WriteLine("{0,4} {1}", lnr, s);
				while (cur != null && cur.line == lnr) {
					Display(s, cur); cur = cur.next;
				}
				lnr++; s = GetLine();
			}
			if (cur != null) {
				mergedList.WriteLine("{0,4}", lnr);
				while (cur != null) {
					Display(s, cur); cur = cur.next;
				}
			}
			mergedList.WriteLine();
			mergedList.WriteLine(count + " errors detected");
			if (warns > 0) mergedList.WriteLine(warns + " warnings detected");
			mergedList.Close();
		}
		switch (count) {
			case 0 : Console.WriteLine("Parsed correctly"); break;
			case 1 : Console.WriteLine("1 error detected"); break;
			default: Console.WriteLine(count + " errors detected"); break;
		}
		if (warns > 0) Console.WriteLine(warns + " warnings detected");
		if ((count > 0 || warns > 0) && mergeErrors) Console.WriteLine("see " + listName);
	}

	public static void StoreError (int line, int col, string s) {
		if (mergeErrors) {
			ErrorRec latest = new ErrorRec(line, col, s);
			if (first == null) first = latest; else last.next = latest;
			last = latest;
		} else Console.WriteLine(errMsgFormat, fileName, line, col, s);
	}

	public static void SynErr (int line, int col, int n) {
		string s;
		switch (n) {
			case 0: s = "EOF expected"; break;
			case 1: s = "ident expected"; break;
			case 2: s = "EOL expected"; break;
			case 3: s = "\"=\" expected"; break;
			case 4: s = "\";\" expected"; break;
			case 5: s = "\"||\" expected"; break;
			case 6: s = "\"or\" expected"; break;
			case 7: s = "\"(\" expected"; break;
			case 8: s = "\")\" expected"; break;
			case 9: s = "\"&&\" expected"; break;
			case 10: s = "\"and\" expected"; break;
			case 11: s = "\"not\" expected"; break;
			case 12: s = "\"!\" expected"; break;
			case 13: s = "??? expected"; break;
			case 14: s = "invalid booleanExpr"; break;
			case 15: s = "invalid andExpr"; break;
			case 16: s = "invalid notExpr"; break;

			default: s = "error " + n; break;
		}
		StoreError(line, col, s);
		count++;
	}

	public static void SemErr (int line, int col, int n) {
		StoreError(line, col, ("error " + n));
		count++;
	}

	public static void Error (int line, int col, string s) {
		StoreError(line, col, s);
		count++;
	}

	public static void Error (string s) {
		if (mergeErrors) mergedList.WriteLine(s); else Console.WriteLine(s);
		count++;
	}

	public static void Warn (int line, int col, string s) {
		StoreError(line, col, s);
		warns++;
	}

	public static void Warn (string s) {
		if (mergeErrors) mergedList.WriteLine(s); else Console.WriteLine(s);
		warns++;
	}

	public static void Exception (string s) {
		Console.WriteLine(s);
		System.Environment.Exit(1);
	}

} // end Errors

} // end namespace
