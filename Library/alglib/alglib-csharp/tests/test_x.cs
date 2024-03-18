#pragma warning disable 219
#pragma warning disable 162
using System;
public class MemoryLeaksTest : System.Runtime.ConstrainedExecution.CriticalFinalizerObject
{
    public int dummy;
    public MemoryLeaksTest()
    {
        dummy = 0;
    }
    ~MemoryLeaksTest()
    {
        if( System.Environment.Version.Major<5 )
        {
            alglib.free_disposed_items();
            long cnt = alglib.alloc_counter();
            System.Console.WriteLine("Allocation counter checked... "+(cnt==0 ? "OK" : "FAILED"));
            if( cnt!=0 )
                System.Environment.ExitCode = 1;
        }
        else
            System.Console.WriteLine("Allocation counter check skipped (NET 5+)");
    }
}

public class XTest
{
    public class paracbck_rosenbrock_problem
    {
        public int n;
        public double parameter;
        
        public int  countdown;
        public bool raise_aperror;
        public int  delay;
        public int  callbacks_running;
        public alglib.smp.ae_lock problem_lock;
        public bool parallelism_detected;
        
        public paracbck_rosenbrock_problem(int _n, double _p)
        {
            n = _n;
            parameter = _p;
            countdown = 0;
            raise_aperror = true;
            delay = 10;
            callbacks_running = 0;
            parallelism_detected = false;
            alglib.smp.ae_init_lock(ref problem_lock);
        }
    }

    public static void paracbck_rosenbrock_func(double[] vars, ref double func, object obj)
    {
        paracbck_rosenbrock_problem problem = (paracbck_rosenbrock_problem)obj;
        
        //
        // Compute target
        //
        func = 0;
        for(int i=0; i<problem.n-1; i++)
            func += problem.parameter*System.Math.Pow(vars[i+1]-vars[i]*vars[i],2) + System.Math.Pow(1-vars[i],2);
        
        //
        // Decrease countdown counter, raise exception if needed
        //
        bool raise_exception = false;
        alglib.smp.ae_acquire_lock(problem.problem_lock);
        if( problem.countdown>0 )
        {
            problem.countdown--;
            raise_exception = problem.countdown==0;
        }
        alglib.smp.ae_release_lock(problem.problem_lock);
        if( raise_exception )
        {
            if( problem.raise_aperror )
                throw new alglib.alglibexception("test ap_error");
            else
                throw new System.Exception("test string");
        }
        
        //
        // Increase number of running callbacks, delay, decrease number of running callbacks.
        // Check that there are running callbacks while waiting.
        //
        int t0 = System.Environment.TickCount;
        alglib.smp.ae_acquire_lock(problem.problem_lock);
        problem.callbacks_running++;
        alglib.smp.ae_release_lock(problem.problem_lock);
        while( System.Environment.TickCount-t0<problem.delay )
        {
            alglib.smp.ae_acquire_lock(problem.problem_lock);
            if( problem.callbacks_running>1 )
                problem.parallelism_detected = true;
            alglib.smp.ae_release_lock(problem.problem_lock);
        }
        alglib.smp.ae_acquire_lock(problem.problem_lock);
        problem.callbacks_running--;
        alglib.smp.ae_release_lock(problem.problem_lock);
    }

    public class paracbck_lsfit_f0_problem
    {
        public int nparams;
        public int ncoord;
        public double[,] a;
        public double[] y;
        public int countdown;
        public bool raise_aperror;
        public int delay;
        public int callbacks_running;
        public alglib.smp.ae_lock problem_lock;
        public bool parallelism_detected;
        
        public paracbck_lsfit_f0_problem(int _p, int coord)
        {
            ncoord = coord;
            nparams = _p;
            countdown = 0;
            raise_aperror = true;
            delay = 10;
            callbacks_running = 0;
            parallelism_detected = false;
            //
            alglib.hqrndstate rs;
            double[] c;
            //
            alglib.smp.ae_init_lock(ref problem_lock);
            //
            alglib.hqrndseed(436444, 935774, out rs);
            alglib.hqrndnormalm(rs, nparams, ncoord, out a);
            alglib.hqrndnormalv(rs, nparams, out c);
            y = new double[nparams];
            for(int i=0; i<nparams; i++)
            {
                y[i] = 0;
                if( ncoord<=nparams )
                {
                    for(int j=0; j<ncoord; j++)
                        y[i] += a[i,j]*c[j];
                    for(int j=ncoord; j<nparams; j++)
                        y[i] += c[j];
                }
                else
                {
                    for(int j=0; j<nparams-1; j++)
                        y[i] += a[i,j]*c[j];
                    double v = 0;
                    for(int j=nparams-1; j<ncoord; j++)
                        v += a[i,j];
                    y[i] += c[nparams-1]*v;
                }
            }
        }
    }
    
    public static void paracbck_lsfitf0_func(double[] vars, double[] coord, ref double func, object obj)
    {
        paracbck_lsfit_f0_problem problem = (paracbck_lsfit_f0_problem)obj;
        
        //
        // Compute target
        //
        func = 0;
        if( problem.ncoord<=problem.nparams )
        {
            for(int j=0; j<problem.ncoord; j++)
                func += coord[j]*vars[j];
            for(int j=problem.ncoord; j<problem.nparams; j++)
                func += vars[j];
        }
        else
        {
            for(int j=0; j<problem.nparams-1; j++)
                func += coord[j]*vars[j];
            double v = 0;
            for(int j=problem.nparams-1; j<problem.ncoord; j++)
                v += coord[j];
            func += vars[problem.nparams-1]*v;
        }
        
        //
        // Decrease countdown counter, raise exception if needed
        //
        bool raise_exception = false;
        alglib.smp.ae_acquire_lock(problem.problem_lock);
        if( problem.countdown>0 )
        {
            problem.countdown--;
            raise_exception = problem.countdown==0;
        }
        alglib.smp.ae_release_lock(problem.problem_lock);
        if( raise_exception )
        {
            if( problem.raise_aperror )
                throw new alglib.alglibexception("test ap_error");
            else
                throw new System.Exception("test string");
        }
        
        //
        // Increase number of running callbacks, delay, decrease number of running callbacks.
        // Check that there are running callbacks while waiting.
        //
        int t0 = System.Environment.TickCount;
        alglib.smp.ae_acquire_lock(problem.problem_lock);
        problem.callbacks_running++;
        alglib.smp.ae_release_lock(problem.problem_lock);
        while( System.Environment.TickCount-t0<problem.delay )
        {
            alglib.smp.ae_acquire_lock(problem.problem_lock);
            if( problem.callbacks_running>1 )
                problem.parallelism_detected = true;
            alglib.smp.ae_release_lock(problem.problem_lock);
        }
        alglib.smp.ae_acquire_lock(problem.problem_lock);
        problem.callbacks_running--;
        alglib.smp.ae_release_lock(problem.problem_lock);
    }


    public static void paracbck_lsfitf0_grad(double[] vars, double[] coord, ref double func, double[] grad, object obj)
    {
        paracbck_lsfit_f0_problem problem = (paracbck_lsfit_f0_problem)obj;
        
        //
        // Compute target
        //
        func = 0;
        if( problem.ncoord<=problem.nparams )
        {
            for(int j=0; j<problem.ncoord; j++)
            {
                func += coord[j]*vars[j];
                grad[j] = coord[j];
            }
            for(int j=problem.ncoord; j<problem.nparams; j++)
            {
                func += vars[j];
                grad[j] = 1;
            }
        }
        else
        {
            for(int j=0; j<problem.nparams-1; j++)
            {
                func += coord[j]*vars[j];
                grad[j] = coord[j];
            }
            double v = 0;
            for(int j=problem.nparams-1; j<problem.ncoord; j++)
                v += coord[j];
            func += vars[problem.nparams-1]*v;
            grad[problem.nparams-1] = v;
        }
        
        //
        // Decrease countdown counter, raise exception if needed
        //
        bool raise_exception = false;
        alglib.smp.ae_acquire_lock(problem.problem_lock);
        if( problem.countdown>0 )
        {
            problem.countdown--;
            raise_exception = problem.countdown==0;
        }
        alglib.smp.ae_release_lock(problem.problem_lock);
        if( raise_exception )
        {
            if( problem.raise_aperror )
                throw new alglib.alglibexception("test ap_error");
            else
                throw new System.Exception("test string");
        }
        
        //
        // Increase number of running callbacks, delay, decrease number of running callbacks.
        // Check that there are running callbacks while waiting.
        //
        int t0 = System.Environment.TickCount;
        alglib.smp.ae_acquire_lock(problem.problem_lock);
        problem.callbacks_running++;
        alglib.smp.ae_release_lock(problem.problem_lock);
        while( System.Environment.TickCount-t0<problem.delay )
        {
            alglib.smp.ae_acquire_lock(problem.problem_lock);
            if( problem.callbacks_running>1 )
                problem.parallelism_detected = true;
            alglib.smp.ae_release_lock(problem.problem_lock);
        }
        alglib.smp.ae_acquire_lock(problem.problem_lock);
        problem.callbacks_running--;
        alglib.smp.ae_release_lock(problem.problem_lock);
    }


    public class paracbck_minlm_f0_problem
    {
        public int nparams;
        public int nfunc;
        public double[,] a;
        public double[] y;
            
        public int countdown;
        public bool raise_aperror;
        public int delay;
        public int callbacks_running;
        public alglib.smp.ae_lock problem_lock;
        public bool parallelism_detected;
        
        public paracbck_minlm_f0_problem(int _p, int ndup)
        {
            nparams = _p;
            nfunc = nparams*ndup;
            countdown = 0;
            raise_aperror = true;
            delay = 10;
            callbacks_running = 0;
            parallelism_detected = false;
            //
            alglib.hqrndstate rs;
            double[] c;
            double[,] a1;
            //
            alglib.smp.ae_init_lock(ref problem_lock);
            //
            alglib.hqrndseed(436444, 935774, out rs);
            alglib.hqrndnormalm(rs, nparams, nparams, out a1);
            alglib.hqrndnormalv(rs, nparams, out c);
            a = new double[nparams*ndup,nparams];
            y = new double[nparams*ndup];
            for(int i=0; i<nparams; i++)
            {
                double v = 0;
                for(int j=0; j<nparams; j++)
                    v += a1[i,j]*c[j];
                for(int k=0; k<ndup; k++)
                {
                    for(int j=0; j<nparams; j++)
                        a[ndup*i+k,j] = a1[i,j];
                    y[ndup*i+k] = v;
                }
            }
        }
    }

    public static void paracbck_minlmf0_fvec(double[] vars, double[] f, object obj)
    {
        paracbck_minlm_f0_problem problem = (paracbck_minlm_f0_problem)obj;
        
        //
        // Compute targets
        //
        for(int i=0; i<problem.nfunc; i++)
        {
            f[i] = 0;
            for(int j=0; j<problem.nparams; j++)
                f[i] += problem.a[i,j]*vars[j];
            f[i] -= problem.y[i];
        }
        
        //
        // Decrease countdown counter, raise exception if needed
        //
        bool raise_exception = false;
        alglib.smp.ae_acquire_lock(problem.problem_lock);
        if( problem.countdown>0 )
        {
            problem.countdown--;
            raise_exception = problem.countdown==0;
        }
        alglib.smp.ae_release_lock(problem.problem_lock);
        if( raise_exception )
        {
            if( problem.raise_aperror )
                throw new alglib.alglibexception("test ap_error");
            else
                throw new System.Exception("test string");
        }
        
        //
        // Increase number of running callbacks, delay, decrease number of running callbacks.
        // Check that there are running callbacks while waiting.
        //
        int t0 = System.Environment.TickCount;
        alglib.smp.ae_acquire_lock(problem.problem_lock);
        problem.callbacks_running++;
        alglib.smp.ae_release_lock(problem.problem_lock);
        while( System.Environment.TickCount-t0<problem.delay )
        {
            alglib.smp.ae_acquire_lock(problem.problem_lock);
            if( problem.callbacks_running>1 )
                problem.parallelism_detected = true;
            alglib.smp.ae_release_lock(problem.problem_lock);
        }
        alglib.smp.ae_acquire_lock(problem.problem_lock);
        problem.callbacks_running--;
        alglib.smp.ae_release_lock(problem.problem_lock);
    }

    public static void paracbck_minlmf0_jac(double[] vars, double[] f, double[,] jac, object obj)
    {
        paracbck_minlm_f0_problem problem = (paracbck_minlm_f0_problem)obj;
        
        //
        // Compute targets
        //
        for(int i=0; i<problem.nfunc; i++)
        {
            f[i] = 0;
            for(int j=0; j<problem.nparams; j++)
            {
                f[i] += problem.a[i,j]*vars[j];
                jac[i,j] = problem.a[i,j];
            }
            f[i] -= problem.y[i];
        }
        
        //
        // Decrease countdown counter, raise exception if needed
        //
        bool raise_exception = false;
        alglib.smp.ae_acquire_lock(problem.problem_lock);
        if( problem.countdown>0 )
        {
            problem.countdown--;
            raise_exception = problem.countdown==0;
        }
        alglib.smp.ae_release_lock(problem.problem_lock);
        if( raise_exception )
        {
            if( problem.raise_aperror )
                throw new alglib.alglibexception("test ap_error");
            else
                throw new System.Exception("test string");
        }
        
        //
        // Increase number of running callbacks, delay, decrease number of running callbacks.
        // Check that there are running callbacks while waiting.
        //
        int t0 = System.Environment.TickCount;
        alglib.smp.ae_acquire_lock(problem.problem_lock);
        problem.callbacks_running++;
        alglib.smp.ae_release_lock(problem.problem_lock);
        while( System.Environment.TickCount-t0<problem.delay )
        {
            alglib.smp.ae_acquire_lock(problem.problem_lock);
            if( problem.callbacks_running>1 )
                problem.parallelism_detected = true;
            alglib.smp.ae_release_lock(problem.problem_lock);
        }
        alglib.smp.ae_acquire_lock(problem.problem_lock);
        problem.callbacks_running--;
        alglib.smp.ae_release_lock(problem.problem_lock);
    }


    public static void Main(string[] args)
    {
        bool _TotalResult = true;
        bool _TestResult;
        System.Console.WriteLine("x-tests. Please wait...");
        System.Console.WriteLine("NET version: {0}", System.Environment.Version.ToString());
        if( System.Environment.Version.Major<5 )
        {
            alglib.alloc_counter_activate();
            System.Console.WriteLine("Allocation counter activated...");
        }
        else
            System.Console.WriteLine("Allocation counter: not supported (NET5+)");
        try
        {
            const int max1d = 70;
            const int max2d = 40;
            
            System.Console.WriteLine("Basic tests:");
            {
                // deallocateimmediately()
                alglib.minlbfgsstate s;
                double[] x = new double[100];
                long cnt0, cnt1;
                cnt0 = alglib.alloc_counter();
                alglib.minlbfgscreate(x.Length, 10, x, out s);
                alglib.deallocateimmediately(ref s);
                cnt1 = alglib.alloc_counter();
                _TestResult = cnt1<=cnt0;
                System.Console.WriteLine("* deallocateimmediately()    "+(_TestResult ? " OK" : " FAILED"));
                _TotalResult = _TotalResult && _TestResult;
            }
            {
                // boolean 1D arrays (this test checks both interface and ref/out conventions used by ALGLIB)
                int n, i, cnt;
                _TestResult = true;
                for(n=0; n<=max1d; n++)
                {
                    bool[] arr0 = new bool[n];
                    bool[] arr1 = new bool[n];
                    bool[] arr2 = new bool[n];
                    bool[] arr3 = null;
                    cnt = 0;
                    for(i=0; i<n; i++)
                    {
                        arr0[i] = alglib.math.randomreal()>0.5;
                        arr1[i] = arr0[i];
                        arr2[i] = arr0[i];
                        if( arr0[i] )
                            cnt++;
                    }
                    _TestResult = _TestResult && (alglib.xdebugb1count(arr0)==cnt);
                    alglib.xdebugb1not(arr1);
                    if( alglib.ap.len(arr1)==n )
                    {
                        for(i=0; i<n; i++)
                            _TestResult = _TestResult && (arr1[i]==!arr0[i]);
                    }
                    else
                        _TestResult = false;
                    alglib.xdebugb1appendcopy(ref arr2);
                    if( alglib.ap.len(arr2)==2*n )
                    {
                        for(i=0; i<2*n; i++)
                            _TestResult = _TestResult && (arr2[i]==arr0[i%n]);
                    }
                    else
                        _TestResult = false;
                    alglib.xdebugb1outeven(n, out arr3);
                    if( alglib.ap.len(arr3)==n )
                    {
                        for(i=0; i<n; i++)
                            _TestResult = _TestResult && (arr3[i]==(i%2==0));
                    }
                    else
                        _TestResult = false;
                }
                System.Console.WriteLine("* boolean 1D arrays          "+(_TestResult ? " OK" : " FAILED"));
                _TotalResult = _TotalResult && _TestResult;
            }
            {
                // integer 1D arrays (this test checks both interface and ref/out conventions used by ALGLIB)
                int n, i, sum;
                _TestResult = true;
                for(n=0; n<=max1d; n++)
                {
                    int[] arr0 = new int[n];
                    int[] arr1 = new int[n];
                    int[] arr2 = new int[n];
                    int[] arr3 = null;
                    sum = 0;
                    for(i=0; i<n; i++)
                    {
                        arr0[i] = alglib.math.randominteger(10);
                        arr1[i] = arr0[i];
                        arr2[i] = arr0[i];
                        sum+=arr0[i];
                    }
                    _TestResult = _TestResult && (alglib.xdebugi1sum(arr0)==sum);
                    alglib.xdebugi1neg(arr1);
                    if( alglib.ap.len(arr1)==n )
                    {
                        for(i=0; i<n; i++)
                            _TestResult = _TestResult && (arr1[i]==-arr0[i]);
                    }
                    else
                        _TestResult = false;
                    alglib.xdebugi1appendcopy(ref arr2);
                    if( alglib.ap.len(arr2)==2*n )
                    {
                        for(i=0; i<2*n; i++)
                            _TestResult = _TestResult && (arr2[i]==arr0[i%n]);
                    }
                    else
                        _TestResult = false;
                    alglib.xdebugi1outeven(n,out arr3);
                    if( alglib.ap.len(arr3)==n )
                    {
                        for(i=0; i<n; i++)
                            if( i%2==0 )
                                _TestResult = _TestResult && (arr3[i]==i);
                            else
                                _TestResult = _TestResult && (arr3[i]==0);
                    }
                    else
                        _TestResult = false;
                }
                System.Console.WriteLine("* integer 1D arrays          "+(_TestResult ? " OK" : " FAILED"));
                _TotalResult = _TotalResult && _TestResult;
            }
            {
                // real 1D arrays (this test checks both interface and ref/out conventions used by ALGLIB)
                int n, i;
                double sum;
                _TestResult = true;
                for(n=0; n<=max1d; n++)
                {
                    double[] arr0 = new double[n];
                    double[] arr1 = new double[n];
                    double[] arr2 = new double[n];
                    double[] arr3 = null;
                    sum = 0;
                    for(i=0; i<n; i++)
                    {
                        arr0[i] = alglib.math.randomreal()-0.5;
                        arr1[i] = arr0[i];
                        arr2[i] = arr0[i];
                        sum+=arr0[i];
                    }
                    _TestResult = _TestResult && (Math.Abs(alglib.xdebugr1sum(arr0)-sum)<1.0E-10);
                    alglib.xdebugr1neg(arr1);
                    if( alglib.ap.len(arr1)==n )
                    {
                        for(i=0; i<n; i++)
                            _TestResult = _TestResult && (Math.Abs(arr1[i]+arr0[i])<1.0E-10);
                    }
                    else
                        _TestResult = false;
                    alglib.xdebugr1appendcopy(ref arr2);
                    if( alglib.ap.len(arr2)==2*n )
                    {
                        for(i=0; i<2*n; i++)
                            _TestResult = _TestResult && (arr2[i]==arr0[i%n]);
                    }
                    else
                        _TestResult = false;
                    alglib.xdebugr1outeven(n,out arr3);
                    if( alglib.ap.len(arr3)==n )
                    {
                        for(i=0; i<n; i++)
                            if( i%2==0 )
                                _TestResult = _TestResult && (arr3[i]==i*0.25);
                            else
                                _TestResult = _TestResult && (arr3[i]==0);
                    }
                    else
                        _TestResult = false;
                }
                System.Console.WriteLine("* real 1D arrays             "+(_TestResult ? " OK" : " FAILED"));
                _TotalResult = _TotalResult && _TestResult;
            }
            {
                // complex 1D arrays (this test checks both interface and ref/out conventions used by ALGLIB)
                int n, i;
                alglib.complex sum;
                _TestResult = true;
                for(n=0; n<=max1d; n++)
                {
                    alglib.complex[] arr0 = new alglib.complex[n];
                    alglib.complex[] arr1 = new alglib.complex[n];
                    alglib.complex[] arr2 = new alglib.complex[n];
                    alglib.complex[] arr3 = null;
                    sum = 0;
                    for(i=0; i<n; i++)
                    {
                        arr0[i].x = alglib.math.randomreal()-0.5;
                        arr0[i].y = alglib.math.randomreal()-0.5;
                        arr1[i] = arr0[i];
                        arr2[i] = arr0[i];
                        sum+=arr0[i];
                    }
                    _TestResult = _TestResult && (alglib.math.abscomplex(alglib.xdebugc1sum(arr0)-sum)<1.0E-10);
                    alglib.xdebugc1neg(arr1);
                    if( alglib.ap.len(arr1)==n )
                    {
                        for(i=0; i<n; i++)
                            _TestResult = _TestResult && (alglib.math.abscomplex(arr1[i]+arr0[i])<1.0E-10);
                    }
                    else
                        _TestResult = false;
                    alglib.xdebugc1appendcopy(ref arr2);
                    if( alglib.ap.len(arr2)==2*n )
                    {
                        for(i=0; i<2*n; i++)
                            _TestResult = _TestResult && (arr2[i]==arr0[i%n]);
                    }
                    else
                        _TestResult = false;
                    alglib.xdebugc1outeven(n,out arr3);
                    if( alglib.ap.len(arr3)==n )
                    {
                        for(i=0; i<n; i++)
                            if( i%2==0 )
                            {
                                _TestResult = _TestResult && (arr3[i].x==i*0.250);
                                _TestResult = _TestResult && (arr3[i].y==i*0.125);
                            }
                            else
                                _TestResult = _TestResult && (arr3[i]==0);
                    }
                    else
                        _TestResult = false;
                }
                System.Console.WriteLine("* complex 1D arrays          "+(_TestResult ? " OK" : " FAILED"));
                _TotalResult = _TotalResult && _TestResult;
            }
            {
                // boolean 2D arrays (this test checks both interface and ref/out conventions used by ALGLIB)
                int m, n, i, j, cnt;
                _TestResult = true;
                for(n=0; n<=max2d; n++)
                    for(m=0; m<=max2d; m++)
                    {
                        // skip situations when n*m==0, but n!=0 or m!=0
                        if( n*m==0 && (n!=0 || m!=0) )
                            continue;
                        
                        // proceed to testing
                        bool[,] arr0 = new bool[m,n];
                        bool[,] arr1 = new bool[m,n];
                        bool[,] arr2 = new bool[m,n];
                        bool[,] arr3 = null;
                        cnt = 0;
                        for(i=0; i<m; i++)
                            for(j=0; j<n; j++)
                            {
                                arr0[i,j] = alglib.math.randomreal()>0.5;
                                arr1[i,j] = arr0[i,j];
                                arr2[i,j] = arr0[i,j];
                                if( arr0[i,j] )
                                    cnt++;
                            }
                        _TestResult = _TestResult && (alglib.xdebugb2count(arr0)==cnt);
                        alglib.xdebugb2not(arr1);
                        if( alglib.ap.rows(arr1)==m && alglib.ap.cols(arr1)==n )
                        {
                            for(i=0; i<m; i++)
                                for(j=0; j<n; j++)
                                    _TestResult = _TestResult && (arr1[i,j]==!arr0[i,j]);
                        }
                        else
                            _TestResult = false;
                        alglib.xdebugb2transpose(ref arr2);
                        if( alglib.ap.rows(arr2)==n && alglib.ap.cols(arr2)==m )
                        {
                            for(i=0; i<m; i++)
                                for(j=0; j<n; j++)
                                    _TestResult = _TestResult && (arr2[j,i]==arr0[i,j]);
                        }
                        else
                            _TestResult = false;
                        alglib.xdebugb2outsin(m, n, out arr3);
                        if( alglib.ap.rows(arr3)==m && alglib.ap.cols(arr3)==n )
                        {
                            for(i=0; i<m; i++)
                                for(j=0; j<n; j++)
                                    _TestResult = _TestResult && (arr3[i,j]==(Math.Sin(3*i+5*j)>0));
                        }
                        else
                            _TestResult = false;
                    }
                System.Console.WriteLine("* boolean 2D arrays          "+(_TestResult ? " OK" : " FAILED"));
                _TotalResult = _TotalResult && _TestResult;
            }
            {
                // integer 2D arrays (this test checks both interface and ref/out conventions used by ALGLIB)
                int m, n, i, j;
                int sum;
                _TestResult = true;
                for(n=0; n<=max2d; n++)
                    for(m=0; m<=max2d; m++)
                    {
                        // skip situations when n*m==0, but n!=0 or m!=0
                        if( n*m==0 && (n!=0 || m!=0) )
                            continue;
                        
                        // proceed to testing
                        int[,] arr0 = new int[m,n];
                        int[,] arr1 = new int[m,n];
                        int[,] arr2 = new int[m,n];
                        int[,] arr3 = null;
                        sum = 0;
                        for(i=0; i<m; i++)
                            for(j=0; j<n; j++)
                            {
                                arr0[i,j] = alglib.math.randominteger(10);
                                arr1[i,j] = arr0[i,j];
                                arr2[i,j] = arr0[i,j];
                                sum += arr0[i,j];
                            }
                        _TestResult = _TestResult && (alglib.xdebugi2sum(arr0)==sum);
                        alglib.xdebugi2neg(arr1);
                        if( alglib.ap.rows(arr1)==m && alglib.ap.cols(arr1)==n )
                        {
                            for(i=0; i<m; i++)
                                for(j=0; j<n; j++)
                                    _TestResult = _TestResult && (arr1[i,j]==-arr0[i,j]);
                        }
                        else
                            _TestResult = false;
                        alglib.xdebugi2transpose(ref arr2);
                        if( alglib.ap.rows(arr2)==n && alglib.ap.cols(arr2)==m )
                        {
                            for(i=0; i<m; i++)
                                for(j=0; j<n; j++)
                                    _TestResult = _TestResult && (arr2[j,i]==arr0[i,j]);
                        }
                        else
                            _TestResult = false;
                        alglib.xdebugi2outsin(m, n, out arr3);
                        if( alglib.ap.rows(arr3)==m && alglib.ap.cols(arr3)==n )
                        {
                            for(i=0; i<m; i++)
                                for(j=0; j<n; j++)
                                    _TestResult = _TestResult && (arr3[i,j]==System.Math.Sign(Math.Sin(3*i+5*j)));
                        }
                        else
                            _TestResult = false;
                    }
                System.Console.WriteLine("* integer 2D arrays          "+(_TestResult ? " OK" : " FAILED"));
                _TotalResult = _TotalResult && _TestResult;
            }
            {
                // real 2D arrays (this test checks both interface and ref/out conventions used by ALGLIB)
                int m, n, i, j;
                double sum;
                _TestResult = true;
                for(n=0; n<=max2d; n++)
                    for(m=0; m<=max2d; m++)
                    {
                        // skip situations when n*m==0, but n!=0 or m!=0
                        if( n*m==0 && (n!=0 || m!=0) )
                            continue;
                        
                        // proceed to testing
                        double[,] arr0 = new double[m,n];
                        double[,] arr1 = new double[m,n];
                        double[,] arr2 = new double[m,n];
                        double[,] arr3 = null;
                        sum = 0;
                        for(i=0; i<m; i++)
                            for(j=0; j<n; j++)
                            {
                                arr0[i,j] = alglib.math.randomreal()-0.5;
                                arr1[i,j] = arr0[i,j];
                                arr2[i,j] = arr0[i,j];
                                sum += arr0[i,j];
                            }
                        _TestResult = _TestResult && (System.Math.Abs(alglib.xdebugr2sum(arr0)-sum)<1.0E-10);
                        alglib.xdebugr2neg(arr1);
                        if( alglib.ap.rows(arr1)==m && alglib.ap.cols(arr1)==n )
                        {
                            for(i=0; i<m; i++)
                                for(j=0; j<n; j++)
                                    _TestResult = _TestResult && (arr1[i,j]==-arr0[i,j]);
                        }
                        else
                            _TestResult = false;
                        alglib.xdebugr2transpose(ref arr2);
                        if( alglib.ap.rows(arr2)==n && alglib.ap.cols(arr2)==m )
                        {
                            for(i=0; i<m; i++)
                                for(j=0; j<n; j++)
                                    _TestResult = _TestResult && (arr2[j,i]==arr0[i,j]);
                        }
                        else
                            _TestResult = false;
                        alglib.xdebugr2outsin(m, n, out arr3);
                        if( alglib.ap.rows(arr3)==m && alglib.ap.cols(arr3)==n )
                        {
                            for(i=0; i<m; i++)
                                for(j=0; j<n; j++)
                                    _TestResult = _TestResult && (System.Math.Abs(arr3[i,j]-Math.Sin(3*i+5*j))<1E-10);
                        }
                        else
                            _TestResult = false;
                    }
                System.Console.WriteLine("* real 2D arrays             "+(_TestResult ? " OK" : " FAILED"));
                _TotalResult = _TotalResult && _TestResult;
            }
            {
                // real 2D arrays (this test checks both interface and ref/out conventions used by ALGLIB)
                int m, n, i, j;
                alglib.complex sum;
                _TestResult = true;
                for(n=0; n<=max2d; n++)
                    for(m=0; m<=max2d; m++)
                    {
                        // skip situations when n*m==0, but n!=0 or m!=0
                        if( n*m==0 && (n!=0 || m!=0) )
                            continue;
                        
                        // proceed to testing
                        alglib.complex[,] arr0 = new alglib.complex[m,n];
                        alglib.complex[,] arr1 = new alglib.complex[m,n];
                        alglib.complex[,] arr2 = new alglib.complex[m,n];
                        alglib.complex[,] arr3 = null;
                        sum = 0;
                        for(i=0; i<m; i++)
                            for(j=0; j<n; j++)
                            {
                                arr0[i,j].x = alglib.math.randomreal()-0.5;
                                arr0[i,j].y = alglib.math.randomreal()-0.5;
                                arr1[i,j] = arr0[i,j];
                                arr2[i,j] = arr0[i,j];
                                sum += arr0[i,j];
                            }
                        _TestResult = _TestResult && (alglib.math.abscomplex(alglib.xdebugc2sum(arr0)-sum)<1.0E-10);
                        alglib.xdebugc2neg(arr1);
                        if( alglib.ap.rows(arr1)==m && alglib.ap.cols(arr1)==n )
                        {
                            for(i=0; i<m; i++)
                                for(j=0; j<n; j++)
                                    _TestResult = _TestResult && (arr1[i,j]==-arr0[i,j]);
                        }
                        else
                            _TestResult = false;
                        alglib.xdebugc2transpose(ref arr2);
                        if( alglib.ap.rows(arr2)==n && alglib.ap.cols(arr2)==m )
                        {
                            for(i=0; i<m; i++)
                                for(j=0; j<n; j++)
                                    _TestResult = _TestResult && (arr2[j,i]==arr0[i,j]);
                        }
                        else
                            _TestResult = false;
                        alglib.xdebugc2outsincos(m, n, out arr3);
                        if( alglib.ap.rows(arr3)==m && alglib.ap.cols(arr3)==n )
                        {
                            for(i=0; i<m; i++)
                                for(j=0; j<n; j++)
                                {
                                    _TestResult = _TestResult && (System.Math.Abs(arr3[i,j].x-Math.Sin(3*i+5*j))<1E-10);
                                    _TestResult = _TestResult && (System.Math.Abs(arr3[i,j].y-Math.Cos(3*i+5*j))<1E-10);
                                }
                        }
                        else
                            _TestResult = false;
                    }
                System.Console.WriteLine("* complex 2D arrays          "+(_TestResult ? " OK" : " FAILED"));
                _TotalResult = _TotalResult && _TestResult;
            }
            {
                // "biased product / sum" test
                int m, n, i, j;
                double sum;
                _TestResult = true;
                for(n=1; n<=max2d; n++)
                    for(m=1; m<=max2d; m++)
                    {
                        // proceed to testing
                        double[,] a = new double[m,n];
                        double[,] b = new double[m,n];
                        bool[,]   c = new bool[m,n];
                        sum = 0;
                        for(i=0; i<m; i++)
                            for(j=0; j<n; j++)
                            {
                                a[i,j] = alglib.math.randomreal()-0.5;
                                b[i,j] = alglib.math.randomreal()-0.5;
                                c[i,j] = alglib.math.randomreal()>0.5;
                                if( c[i,j] )
                                    sum += a[i,j]*(1+b[i,j]);
                            }
                        _TestResult = _TestResult && (Math.Abs(alglib.xdebugmaskedbiasedproductsum(m,n,a,b,c)-sum)<1.0E-10);
                    }
                System.Console.WriteLine("* multiple arrays            "+(_TestResult ? " OK" : " FAILED"));
                _TotalResult = _TotalResult && _TestResult;
            }
            
            //
            // Test new RCOMM-V2 interface (parallel optimizers)
            //
            System.Console.WriteLine("Parallel optimizers:");
            if( System.Environment.ProcessorCount>1 && alglib.ap.is_commercial )
            {
                double diff_step = 0.0001;
                double rosenbrock_parameter = 2.0;
                double ftol_lbfgs = 1.0e-10;
                int i, n, npoints, nparams, ncoord;
                double f = 0;
                double[] x0, xf;
                bool test_failed;
                
                //
                // Test LBFGS serial and parallel numerical differentiation
                //
                int local_counter;
                test_failed = false;
                n = 2;
                local_counter = 0;
                for(int global_cb_parallelism=0; global_cb_parallelism<=2; global_cb_parallelism++)
                    for(int local_cb_parallelism=0; local_cb_parallelism<=2; local_cb_parallelism++)
                    {
                        //
                        // determine parallelism parameters
                        //
                        alglib.xparams[] _cbk_map  = new alglib.xparams[]{ alglib.xdefault, alglib.serial_callbacks, alglib.parallel_callbacks };
                        alglib.xparams[] _add1_map = new alglib.xparams[]{ alglib.xdefault, alglib.serial, alglib.parallel, alglib.xdefault };
                        alglib.xparams[] _add2_map = new alglib.xparams[]{ alglib.serial, alglib.parallel, alglib.xdefault, alglib.parallel, alglib.serial };
                        //
                        alglib.xparams xglb = _cbk_map[global_cb_parallelism] | _add1_map[local_counter%4];
                        alglib.xparams xloc = _cbk_map[ local_cb_parallelism] | _add2_map[local_counter%5];
                        bool are_callbacks_parallel = local_cb_parallelism==2 || (local_cb_parallelism==0 && global_cb_parallelism==2);
                        local_counter++;
                        
                        //
                        // Solve with current parallelism settings
                        //
                        alglib.minlbfgsstate state0;
                        alglib.minlbfgsreport rep0;
                        paracbck_rosenbrock_problem problem0 = new paracbck_rosenbrock_problem(n,rosenbrock_parameter);
                        x0 = new double[n];
                        for(i=0; i<n; i++)
                            x0[i] = 0;
                        int t0 = System.Environment.TickCount;
                        alglib.minlbfgscreatef(n, System.Math.Min(n,2), x0, diff_step, out state0);
                        alglib.setglobalthreading(xglb);
                        alglib.minlbfgsoptimize(state0, paracbck_rosenbrock_func, null, problem0, xloc);
                        alglib.minlbfgsresults(state0, out xf, out rep0);
                        paracbck_rosenbrock_func(xf, ref f, problem0);
                        if( are_callbacks_parallel && !problem0.parallelism_detected )
                        {
                            System.Console.WriteLine(">>> MINLBFGS: alglib::parallel_callbacks are processed sequentially");
                            test_failed =  true;
                        }
                        if( problem0.parallelism_detected && !are_callbacks_parallel )
                        {
                            System.Console.WriteLine(">>> MINLBFGS: alglib::serial_callbacks are parallelized");
                            test_failed =  true;
                        }
                        if( f>ftol_lbfgs )
                        {
                            System.Console.WriteLine(">>> MINLBFGS: optimizer failed");
                            test_failed =  true;
                        }
                    }
                alglib.setglobalthreading(alglib.xdefault); // restore defaults
                {
                    //
                    // Test various crash scenarios
                    //
                    for(int cc=1; cc<=3; cc++)
                        for(int cbtype=0; cbtype<2; cbtype++)
                        {
                            alglib.minlbfgsstate state0;
                            alglib.minlbfgsreport rep0;
                            paracbck_rosenbrock_problem problem0 = new paracbck_rosenbrock_problem(n,rosenbrock_parameter);
                            problem0.countdown = cc;
                            x0 = new double[n];
                            for(i=0; i<n; i++)
                                x0[i] = 0;
                            alglib.minlbfgscreatef(n, System.Math.Min(n,2), x0, diff_step, out state0);
                            try
                            {
                                alglib.minlbfgsoptimize(state0, paracbck_rosenbrock_func, null, problem0, cbtype==0 ? alglib.serial_callbacks : alglib.parallel_callbacks);
                                test_failed =  true;
                                System.Console.WriteLine(">>> MINLBFGS: incorrect exception handling in callbacks");
                            }
                            catch(alglib.alglibexception e)
                            {
                            }
                        }
                }
                System.Console.WriteLine("* minlbfgs (D-protocol)       "+(test_failed ? "FAILED" : "OK"));
                _TotalResult = _TotalResult && !test_failed;
                
                //
                // Test MINLM
                //
                test_failed = false;
                for(int ptype=0; ptype<=1; ptype++)
                    for(int cbtype=0; cbtype<=1; cbtype++)
                        for(int ndup=1; ndup<=2; ndup++)
                            {   
                                //
                                // Test serial and parallel numerical differentiation, serial and parallel Jacobians.
                                //
                                nparams = 3;
                                double ftol = 0.02;
                                int    maxits = 2; // just two iterations in order to be able to catch convergence problems due to errors in gradients
                                //
                                alglib.minlmstate state0;
                                alglib.minlmreport rep0;
                                double[] cv;
                                paracbck_minlm_f0_problem problem0 = new paracbck_minlm_f0_problem(nparams, ndup);
                                x0 = new double[nparams];
                                for(i=0; i<nparams; i++)
                                    x0[i] = 0;
                                
                                // modify problem statement according to the duplication factor: either use raw matrix
                                // produced by the generator, or replace each point f(x)=y with a pair f(x)=y-EPS / f(x)=y+EPS
                                if( cbtype==0 )
                                {
                                    problem0.delay = 10;
                                    alglib.minlmcreatev(nparams, alglib.ap.rows(problem0.a), x0, diff_step, out state0);
                                    alglib.minlmsetcond(state0, 0.0, maxits);
                                    alglib.minlmoptimize(state0, paracbck_minlmf0_fvec, null, problem0, ptype==0 ? alglib.serial_callbacks : alglib.parallel_callbacks);
                                }
                                else
                                {
                                    problem0.delay = 10;
                                    alglib.minlmcreatevj(nparams, alglib.ap.rows(problem0.a), x0, out state0);
                                    alglib.minlmsetcond(state0, 0.0, maxits);
                                    alglib.minlmoptimize(state0, paracbck_minlmf0_fvec, paracbck_minlmf0_jac, null, problem0, ptype==0 ? alglib.serial_callbacks : alglib.parallel_callbacks);
                                }
                                alglib.minlmresults(state0, out xf, out rep0);
                                cv = new double[alglib.ap.rows(problem0.a)];
                                paracbck_minlmf0_fvec(xf, cv, problem0);
                                for(i=0; i<alglib.ap.rows(problem0.a); i++)
                                    if( System.Math.Abs(cv[i])>ftol )
                                    {
                                        System.Console.WriteLine(">>> MINLM: optimizer failed");
                                        System.Console.WriteLine("{0}", System.Math.Abs(cv[i]));
                                        test_failed = true;
                                    }
                                if( ptype==0 && problem0.parallelism_detected )
                                {
                                    System.Console.WriteLine(">>> MINLM: alglib::serial_callbacks are processed in parallel");
                                    test_failed =  true;
                                }
                                if( cbtype==0 && ptype!=0 && !problem0.parallelism_detected )
                                {
                                    System.Console.WriteLine(">>> MINLM: alglib::parallel_callbacks are processed serially");
                                    test_failed =  true;
                                }
                            }
                System.Console.WriteLine("* minlm (B-, D- protocols)    "+(test_failed ? "FAILED" : "OK"));
                _TotalResult = _TotalResult && !test_failed;
                
                //
                // Test LSFIT
                //
                test_failed = false;
                for(int ptype=0; ptype<=1; ptype++)
                    for(int cbtype=0; cbtype<=1; cbtype++)
                        for(int ndup=1; ndup<=2; ndup++)
                            for(int stype=-1; stype<=+1; stype++)
                            {
                                //
                                // Test serial and parallel numerical differentiation, serial and parallel gradients.
                                //
                                nparams = 3;
                                ncoord  = nparams+stype;
                                double ftol = 0.02;
                                int    maxits = 2; // just two iterations in order to be able to catch convergence problems due to errors in gradients
                                //
                                alglib.lsfitstate state0;
                                alglib.lsfitreport rep0;
                                double[] cv;
                                paracbck_lsfit_f0_problem problem0 = new paracbck_lsfit_f0_problem(nparams, ncoord);
                                x0 = new double[nparams];
                                for(i=0; i<nparams; i++)
                                    x0[i] = 0;
                                
                                // modify problem statement according to the duplication factor: either use raw matrix
                                // produced by the generator, or replace each point f(x)=y with a pair f(x)=y-EPS / f(x)=y+EPS
                                double[,] moda = (double[,])problem0.a.Clone();
                                double[]  mody = (double[])problem0.y.Clone();
                                if( ndup==2 )
                                {
                                    moda = new double[2*alglib.ap.rows(problem0.a), alglib.ap.cols(problem0.a)];
                                    mody = new double[2*alglib.ap.rows(problem0.a)];
                                    for(i=0; i<alglib.ap.rows(problem0.a); i++)
                                    {
                                        for(int j=0; j<alglib.ap.cols(problem0.a); j++)
                                        {
                                            moda[2*i+0,j] = problem0.a[i,j];
                                            moda[2*i+1,j] = problem0.a[i,j];
                                        }
                                        double eps = System.Math.Sin(117*i+0.65);
                                        mody[2*i+0] = problem0.y[i]+eps;
                                        mody[2*i+1] = problem0.y[i]-eps;
                                    }
                                }
                                if( cbtype==0 )
                                {
                                    problem0.delay = 10;
                                    alglib.lsfitcreatef(moda, mody, x0, alglib.ap.rows(moda), alglib.ap.cols(moda), nparams, diff_step, out state0);
                                    alglib.lsfitsetcond(state0, 0.0, maxits);
                                    alglib.lsfitfit(state0, paracbck_lsfitf0_func, null, problem0, ptype==0 ? alglib.serial_callbacks : alglib.parallel_callbacks);
                                }
                                else
                                {
                                    problem0.delay = 10;
                                    alglib.lsfitcreatefg(moda, mody, x0, alglib.ap.rows(moda), alglib.ap.cols(moda), nparams, out state0);
                                    alglib.lsfitsetcond(state0, 0.0, maxits);
                                    alglib.lsfitfit(state0, paracbck_lsfitf0_func, paracbck_lsfitf0_grad, null, problem0, ptype==0 ? alglib.serial_callbacks : alglib.parallel_callbacks);
                                }
                                alglib.lsfitresults(state0, out xf, out rep0);
                                cv = new double[alglib.ap.cols(problem0.a)];
                                for(i=0; i<alglib.ap.rows(problem0.a); i++)
                                {
                                    for(int j=0; j<alglib.ap.cols(problem0.a); j++)
                                        cv[j] = problem0.a[i,j];
                                    f = 0;
                                    paracbck_lsfitf0_func(xf, cv, ref f, problem0);
                                    if( System.Math.Abs(f-problem0.y[i])>ftol )
                                    {
                                        System.Console.WriteLine(">>> LSFIT: optimizer failed");
                                        System.Console.WriteLine("{0}", System.Math.Abs(f-problem0.y[i]));
                                        test_failed = true;
                                    }
                                }
                                if( ptype==0 && problem0.parallelism_detected )
                                {
                                    System.Console.WriteLine(">>> LSFIT: alglib::serial_callbacks are processed in parallel");
                                    test_failed =  true;
                                }
                                if( ptype!=0 && !problem0.parallelism_detected )
                                {
                                    System.Console.WriteLine(">>> LSFIT: alglib::parallel_callbacks are processed serially");
                                    test_failed =  true;
                                }
                            }
                System.Console.WriteLine("* lsfit (B-, D- protocols)    "+(test_failed ? "FAILED" : "OK"));
                _TotalResult = _TotalResult && !test_failed;
            }
            else
                System.Console.WriteLine("* tests                       SKIPPED (free edition or just one core found)");

            //
            // Test multithreading-related settings
            //
            // For this test we measure performance of large NxNxN GEMMs
            // with different threading settings.
            //
            {
                System.Console.WriteLine("SMP settings vs GEMM speedup:");
                if( alglib.smp.cores_count>1 )
                {
                    bool passed = true;
                    int n = 800, mintime = 2000, t0;
                    ulong default_global_threading = alglib.ae_get_global_threading();
                    int default_nworkers = alglib.getnworkers();
                    double time_default          = 0,
                           time_glob_ser         = 0,
                           time_glob_smp         = 0,
                           time_glob_ser_loc_ser = 0,
                           time_glob_ser_loc_smp = 0,
                           time_glob_smp_loc_ser = 0,
                           time_glob_smp_loc_smp = 0,
                           time_glob_smp_nw1     = 0;
                    try
                    {
                        // allocate temporary matrices
                        double[,] a = new double[n,n], b = new double[n,n], c = new double[n,n];
                        int i, j;
                        for(i=0; i<n; i++)
                            for(j=0; j<n; j++)
                            {
                                a[i,j] = alglib.math.randomreal()-0.5;
                                b[i,j] = alglib.math.randomreal()-0.5;
                                c[i,j] = 0.0;
                            }
                        
                        // measure time; interleave measurements with different settings in order to
                        // reduce variance of results
                        while(time_default<mintime)
                        {
                            // default threading
                            t0 = System.Environment.TickCount;
                            alglib.rmatrixgemm(
                                n, n, n,
                                1.0,
                                a, 0, 0, 0,
                                b, 0, 0, 0,
                                0.0,
                                c, 0, 0);
                            time_default += System.Environment.TickCount-t0;
                            alglib.ae_set_global_threading(default_global_threading); // restore
                            
                            // global serial
                            t0 = System.Environment.TickCount;
                            alglib.setglobalthreading(alglib.serial);
                            alglib.rmatrixgemm(
                                n, n, n,
                                1.0,
                                a, 0, 0, 0,
                                b, 0, 0, 0,
                                0.0,
                                c, 0, 0);
                            time_glob_ser += System.Environment.TickCount-t0;
                            alglib.ae_set_global_threading(default_global_threading); // restore
                            
                            // global parallel
                            t0 = System.Environment.TickCount;
                            alglib.setglobalthreading(alglib.parallel);
                            alglib.rmatrixgemm(
                                n, n, n,
                                1.0,
                                a, 0, 0, 0,
                                b, 0, 0, 0,
                                0.0,
                                c, 0, 0);
                            time_glob_smp += System.Environment.TickCount-t0;
                            alglib.ae_set_global_threading(default_global_threading); // restore
                            
                            // global serial, local serial
                            t0 = System.Environment.TickCount;
                            alglib.setglobalthreading(alglib.serial);
                            alglib.rmatrixgemm(
                                n, n, n,
                                1.0,
                                a, 0, 0, 0,
                                b, 0, 0, 0,
                                0.0,
                                c, 0, 0,
                                alglib.serial);
                            time_glob_ser_loc_ser += System.Environment.TickCount-t0;
                            alglib.ae_set_global_threading(default_global_threading); // restore
                            
                            // global serial, local parallel
                            t0 = System.Environment.TickCount;
                            alglib.setglobalthreading(alglib.serial);
                            alglib.rmatrixgemm(
                                n, n, n,
                                1.0,
                                a, 0, 0, 0,
                                b, 0, 0, 0,
                                0.0,
                                c, 0, 0,
                                alglib.parallel);
                            time_glob_ser_loc_smp += System.Environment.TickCount-t0;
                            alglib.ae_set_global_threading(default_global_threading); // restore
                            
                            // global parallel, local serial
                            t0 = System.Environment.TickCount;
                            alglib.setglobalthreading(alglib.parallel);
                            alglib.rmatrixgemm(
                                n, n, n,
                                1.0,
                                a, 0, 0, 0,
                                b, 0, 0, 0,
                                0.0,
                                c, 0, 0,
                                alglib.serial);
                            time_glob_smp_loc_ser += System.Environment.TickCount-t0;
                            alglib.ae_set_global_threading(default_global_threading); // restore
                            
                            // global parallel, local parallel
                            t0 = System.Environment.TickCount;
                            alglib.setglobalthreading(alglib.parallel);
                            alglib.rmatrixgemm(
                                n, n, n,
                                1.0,
                                a, 0, 0, 0,
                                b, 0, 0, 0,
                                0.0,
                                c, 0, 0,
                                alglib.parallel);
                            time_glob_smp_loc_smp += System.Environment.TickCount-t0;
                            alglib.ae_set_global_threading(default_global_threading); // restore
                            
                            // global parallel, nworkers 1
                            t0 = System.Environment.TickCount;
                            alglib.setglobalthreading(alglib.parallel);
                            alglib.setnworkers(1);
                            alglib.rmatrixgemm(
                                n, n, n,
                                1.0,
                                a, 0, 0, 0,
                                b, 0, 0, 0,
                                0.0,
                                c, 0, 0);
                            time_glob_smp_nw1 += System.Environment.TickCount-t0;
                            alglib.ae_set_global_threading(default_global_threading); // restore
                            alglib.setnworkers(default_nworkers);
                        }
                    }
                    catch
                    { passed = false; }
                    System.Console.WriteLine("* default speedup         {0,5:F1}x", 1.0);
                    System.Console.WriteLine("* serial (global)         {0,5:F1}x", time_glob_ser/time_default);
                    System.Console.WriteLine("* serial (local)          {0,5:F1}x", time_glob_ser/time_glob_ser_loc_ser);
                    System.Console.WriteLine("* serial (nworkers=1)     {0,5:F1}x", time_glob_ser/time_glob_smp_nw1);
                    System.Console.WriteLine("* parallel (global)       {0,5:F1}x", time_glob_ser/time_glob_smp);
                    System.Console.WriteLine("* parallel (local) v1     {0,5:F1}x", time_glob_ser/time_glob_ser_loc_smp);
                    passed = passed && (time_glob_ser/time_default         >0.85) && (time_glob_ser/time_default         <1.15);
                    passed = passed && (time_glob_ser/time_glob_ser        >0.85) && (time_glob_ser/time_glob_ser        <1.15);
                    passed = passed && (time_glob_ser/time_glob_ser_loc_ser>0.85) && (time_glob_ser/time_glob_ser_loc_ser<1.15);
                    passed = passed && (time_glob_ser/time_glob_smp_loc_ser>0.85) && (time_glob_ser/time_glob_smp_loc_ser<1.15);
                    passed = passed && (time_glob_ser/time_glob_smp_nw1    >0.85) && (time_glob_ser/time_glob_smp_nw1    <1.15);
                    passed = passed && (time_glob_ser/time_glob_smp        >1.30);
                    passed = passed && (time_glob_ser/time_glob_ser_loc_smp>1.30);
                    passed = passed && (time_glob_ser/time_glob_smp_loc_smp>1.30);
                    System.Console.WriteLine("* test result                 "+(passed ? "OK" : "FAILED (soft failure)"));
                    //
                    // soft failure:
                    // // if( !passed )
                    // //   return 1;
                    //
                }
                else
                    System.Console.WriteLine("* test skipped (no SMP)       "+"??");
            }
            
            
            //////////////////////////////////
            // Advanced tests
            //////
            System.Console.WriteLine("Advanced tests:");

            //
            // Testing CSV functionality
            //
            {
                string csv_name = "alglib-tst-35252-ndg4sf.csv";
                _TestResult = true;
                try
                {
                    // CSV_DEFAULT must be zero
                    _TestResult = _TestResult && alglib.CSV_DEFAULT==0;
                    
                    // absent file - must fail
                    try
                    {
                        double[,] arr;
                        alglib.read_csv("nonexistent123foralgtestinglib", '\t', alglib.CSV_DEFAULT, out arr);
                        _TestResult = false;
                    }
                    catch
                    { }
                    
                    // non-rectangular file - must fail
                    try
                    {
                        double[,] arr;
                        System.IO.File.WriteAllText(csv_name, "a,b,c\r\n1,2");
                        alglib.read_csv(csv_name, ',', alglib.CSV_SKIP_HEADERS, out arr);
                        System.IO.File.Delete(csv_name);
                        _TestResult = false;
                    }
                    catch
                    { }
                    try
                    {
                        double[,] arr;
                        System.IO.File.WriteAllText(csv_name, "a,b,c\r\n1,2,3,4");
                        alglib.read_csv(csv_name, ',', alglib.CSV_SKIP_HEADERS, out arr);
                        System.IO.File.Delete(csv_name);
                        _TestResult = false;
                    }
                    catch
                    { }
                    try
                    {
                        double[,] arr;
                        System.IO.File.WriteAllText(csv_name, "1,2,3,4\n1,2,3\n1,2,3");
                        alglib.read_csv(csv_name, ',', alglib.CSV_DEFAULT, out arr);
                        System.IO.File.Delete(csv_name);
                        _TestResult = false;
                    }
                    catch
                    { }
                    
                    // empty file
                    try
                    {
                        double[,] arr;
                        System.IO.File.WriteAllText(csv_name, "");
                        alglib.read_csv(csv_name, '\t', alglib.CSV_DEFAULT, out arr);
                        System.IO.File.Delete(csv_name);
                        _TestResult = _TestResult && arr.GetLength(0)==0 && arr.GetLength(1)==0;
                    }
                    catch
                    { _TestResult = false; }
                    
                    // one row with header, tab separator
                    try
                    {
                        double[,] arr;
                        System.IO.File.WriteAllText(csv_name, "a\tb\tc\n");
                        alglib.read_csv(csv_name, '\t', alglib.CSV_SKIP_HEADERS, out arr);
                        System.IO.File.Delete(csv_name);
                        _TestResult = _TestResult && arr.GetLength(0)==0 && arr.GetLength(1)==0;
                    }
                    catch
                    { _TestResult = false; }
                    
                    // no header, comma-separated, full stop as decimal point
                    try
                    {
                        double[,] arr;
                        System.IO.File.WriteAllText(csv_name, "1.5,2,3.25\n4,5,6");
                        alglib.read_csv(csv_name, ',', alglib.CSV_DEFAULT, out arr);
                        System.IO.File.Delete(csv_name);
                        _TestResult = _TestResult && alglib.ap.format(arr,2)=="{{1.50,2.00,3.25},{4.00,5.00,6.00}}";
                    }
                    catch
                    { _TestResult = false; }
                    
                    // header, tab-separated, mixed use of comma and full stop as decimal points
                    try
                    {
                        double[,] arr;
                        System.IO.File.WriteAllText(csv_name, "a\tb\tc\n1.5\t2\t3,25\n4\t5.25\t6,1\n");
                        alglib.read_csv(csv_name, '\t', alglib.CSV_SKIP_HEADERS, out arr);
                        System.IO.File.Delete(csv_name);
                        _TestResult = _TestResult && alglib.ap.format(arr,2)=="{{1.50,2.00,3.25},{4.00,5.25,6.10}}";
                    }
                    catch
                    { _TestResult = false; }
                    
                    // header, tab-separated, fixed/exponential, spaces, mixed use of comma and full stop as decimal points
                    try
                    {
                        double[,] arr;
                        System.IO.File.WriteAllText(csv_name, " a\t b \tc\n1,1\t 2.9\t -3.5  \n  1.1E1  \t 2.0E-1 \t-3E+1 \n+1  \t -2\t 3.    \n.1\t-.2\t+.3\n");
                        alglib.read_csv(csv_name, '\t', alglib.CSV_SKIP_HEADERS, out arr);
                        System.IO.File.Delete(csv_name);
                        _TestResult = _TestResult && alglib.ap.format(arr,2)=="{{1.10,2.90,-3.50},{11.00,0.20,-30.00},{1.00,-2.00,3.00},{0.10,-0.20,0.30}}";
                    }
                    catch
                    { _TestResult = false; }
                }
                catch
                {
                    _TestResult = false;
                }
                
                //
                // Report
                //
                System.Console.WriteLine("* CSV support                "+(_TestResult ? " OK" : " FAILED"));
                _TotalResult = _TotalResult && _TestResult;
            }

            //
            // Testing serialization functionality (using kd-trees as playground)
            //
            {
                
                _TestResult = true;
                try
                {
                    // prepare data
                    alglib.hqrndstate rs;
                    alglib.kdtree tree0;
                    double[,] xy, rxy0 = new double[0,0], rxy1 = new double[0,0];
                    double[]  qx;
                    const int npts = 50;
                    const int nx = 2;
                    const int ny = 1;
                    int cnt0, cnt1;
                    alglib.hqrndrandomize(out rs);
                    xy = new double[npts,nx+ny];
                    for(int i=0; i<npts; i++)
                        for(int j=0; j<nx+ny; j++)
                            xy[i,j] = alglib.hqrndnormal(rs);
                    alglib.kdtreebuild(xy, npts, nx, ny, 2, out tree0);
                    qx = new double[nx];
                    
                    try
                    {
                        // test string serialization/unserialization
                        alglib.kdtree tree1;
                        string s;
                        alglib.kdtreeserialize(tree0, out s);
                        alglib.kdtreeunserialize(s, out tree1);
                        for(int i=0; i<100; i++)
                        {
                            for(int j=0; j<nx; j++)
                                qx[j] = alglib.hqrndnormal(rs);
                            cnt0 = alglib.kdtreequeryknn(tree0, qx, 1, true);
                            cnt1 = alglib.kdtreequeryknn(tree1, qx, 1, true);
                            if( (cnt0!=1) || (cnt1!=1) )
                            {
                                _TestResult = false;
                                break;
                            }
                            alglib.kdtreequeryresultsxy(tree0, ref rxy0);
                            alglib.kdtreequeryresultsxy(tree1, ref rxy1);
                            for(int j=0; j<nx+ny; j++)
                                _TestResult = _TestResult && (rxy0[0,j]==rxy1[0,j]);
                        }
                    }
                    catch
                    { _TestResult = false; }
                    
                    try
                    {
                        // test stream serialization/unserialization
                        //
                        // NOTE: we add a few symbols at the beginning and after the end of the data
                        //       in order to test algorithm ability to work in the middle of the stream
                        alglib.kdtree tree1;
                        System.IO.MemoryStream s = new System.IO.MemoryStream();
                        s.WriteByte((byte)'b');
                        s.WriteByte((byte)' ');
                        s.WriteByte((byte)'e');
                        s.WriteByte((byte)'g');
                        alglib.kdtreeserialize(tree0, s);
                        s.WriteByte((byte)'@');
                        s.WriteByte((byte)' ');
                        s.WriteByte((byte)'n');
                        s.WriteByte((byte)'d');
                        s.Seek(0, System.IO.SeekOrigin.Begin);
                        _TestResult = _TestResult && (s.ReadByte()==(byte)'b');
                        _TestResult = _TestResult && (s.ReadByte()==(byte)' ');
                        _TestResult = _TestResult && (s.ReadByte()==(byte)'e');
                        _TestResult = _TestResult && (s.ReadByte()==(byte)'g');
                        alglib.kdtreeunserialize(s, out tree1);
                        _TestResult = _TestResult && (s.ReadByte()==(byte)'@');
                        _TestResult = _TestResult && (s.ReadByte()==(byte)' ');
                        _TestResult = _TestResult && (s.ReadByte()==(byte)'n');
                        _TestResult = _TestResult && (s.ReadByte()==(byte)'d');
                        for(int i=0; i<100; i++)
                        {
                            for(int j=0; j<nx; j++)
                                qx[j] = alglib.hqrndnormal(rs);
                            cnt0 = alglib.kdtreequeryknn(tree0, qx, 1, true);
                            cnt1 = alglib.kdtreequeryknn(tree1, qx, 1, true);
                            if( (cnt0!=1) || (cnt1!=1) )
                            {
                                _TestResult = false;
                                break;
                            }
                            alglib.kdtreequeryresultsxy(tree0, ref rxy0);
                            alglib.kdtreequeryresultsxy(tree1, ref rxy1);
                            for(int j=0; j<nx+ny; j++)
                                _TestResult = _TestResult && (rxy0[0,j]==rxy1[0,j]);
                        }
                    }
                    catch
                    { _TestResult = false; }
                    
                    try
                    {
                        // test string-to-stream serialization/unserialization
                        alglib.kdtree tree1;
                        string s0;
                        alglib.kdtreeserialize(tree0, out s0);
                        System.IO.MemoryStream s1 = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(s0));
                        alglib.kdtreeunserialize(s1, out tree1);
                        for(int i=0; i<100; i++)
                        {
                            for(int j=0; j<nx; j++)
                                qx[j] = alglib.hqrndnormal(rs);
                            cnt0 = alglib.kdtreequeryknn(tree0, qx, 1, true);
                            cnt1 = alglib.kdtreequeryknn(tree1, qx, 1, true);
                            if( (cnt0!=1) || (cnt1!=1) )
                            {
                                _TestResult = false;
                                break;
                            }
                            alglib.kdtreequeryresultsxy(tree0, ref rxy0);
                            alglib.kdtreequeryresultsxy(tree1, ref rxy1);
                            for(int j=0; j<nx+ny; j++)
                                _TestResult = _TestResult && (rxy0[0,j]==rxy1[0,j]);
                        }
                    }
                    catch
                    { _TestResult = false; }
                    
                    try
                    {
                        // test stream-to-string serialization/unserialization
                        alglib.kdtree tree1;
                        System.IO.MemoryStream s0 = new System.IO.MemoryStream();
                        alglib.kdtreeserialize(tree0, s0);
                        s0.Seek(0, System.IO.SeekOrigin.Begin);
                        string s1 = System.Text.Encoding.UTF8.GetString(s0.ToArray());
                        alglib.kdtreeunserialize(s1, out tree1);
                        for(int i=0; i<100; i++)
                        {
                            for(int j=0; j<nx; j++)
                                qx[j] = alglib.hqrndnormal(rs);
                            cnt0 = alglib.kdtreequeryknn(tree0, qx, 1, true);
                            cnt1 = alglib.kdtreequeryknn(tree1, qx, 1, true);
                            if( (cnt0!=1) || (cnt1!=1) )
                            {
                                _TestResult = false;
                                break;
                            }
                            alglib.kdtreequeryresultsxy(tree0, ref rxy0);
                            alglib.kdtreequeryresultsxy(tree1, ref rxy1);
                            for(int j=0; j<nx+ny; j++)
                                _TestResult = _TestResult && (rxy0[0,j]==rxy1[0,j]);
                        }
                    }
                    catch
                    { _TestResult = false; }

                }
                catch
                {
                    _TestResult = false;
                }
                
                //
                // Report
                //
                System.Console.WriteLine("* Serialization (kd-tree)    "+(_TestResult ? " OK" : " FAILED"));
                _TotalResult = _TotalResult && _TestResult;
            }
            
            //////////////////////////////////
            // Test issues from Mantis
            //////
            System.Console.WriteLine("Testing issies from Mantis:");
                
            
            //
            // Task #594 (http://bugs.alglib.net/view.php?id=594) - additional
            // test for correctness of copying of objects. When we copy ALGLIB
            // object, indenendent new copy is created.
            //
            {
                //
                // First, test copying of alglib.multilayerperceptron, which
                // is an "opaque object".
                //
                // Test copy constructors:
                // * copy object with make_copy()
                // * process vector with original network
                // * randomize original network
                // * process vector with copied networks and compare
                //
                alglib.multilayerperceptron net0, net1;
                double[] x  = new double[]{1,2};
                double[] y0 = new double[]{0,0};
                double[] y1 = new double[]{0,0};
                double[] y2 = new double[]{0,0};
                _TestResult = true;
                alglib.mlpcreate0(2, 2, out net0);
                alglib.mlpprocess(net0, x, ref y0);
                net1 = (alglib.multilayerperceptron)net0.make_copy();
                alglib.mlprandomize(net0);
                alglib.mlpprocess(net1, x, ref y1);
                _TestResult = _TestResult && (Math.Abs(y0[0]-y1[0])<1.0E-9) && (Math.Abs(y0[1]-y1[1])<1.0E-9);
                
                //
                // Then, test correctness of copying "records", i.e.
                // objects with publicly visible fields.
                //
                alglib.xdebugrecord1 r0, r1;
                alglib.xdebuginitrecord1(out r0);
                r1 = (alglib.xdebugrecord1)r0.make_copy();
                _TestResult = _TestResult && (r1.i==r0.i);
                _TestResult = _TestResult && (r1.c==r0.c);
                
                _TestResult = _TestResult && (r1.a.Length==2);
                _TestResult = _TestResult && (r0.a.Length==2);
                _TestResult = _TestResult && (r1.a!=r0.a);
                _TestResult = _TestResult && (r1.a[0]==r0.a[0]);
                _TestResult = _TestResult && (r1.a[1]==r0.a[1]);
                
                //
                // Test result
                //
                System.Console.WriteLine("* issue 594                  "+(_TestResult ? " OK" : " FAILED"));
                _TotalResult = _TotalResult && _TestResult;
            }
            
        }
        catch
        {
            System.Console.WriteLine("Unhandled exception was raised!");
            System.Environment.ExitCode = 1;
            return;
        }
        
            
        //////////////////////////////////
        // Backward compatibility tests
        //////
        System.Console.WriteLine("Backward compatibility tests:");

        //
        // Testing RBF storage format
        //
        {
            double eps = 0.0000000001;
            double[] ref_val = new double[]{
                -0.042560546916643,
                 0.942523544654062,
                 0.875197036560778,
                 0.0656948997826632,
                -0.743065973803404,
                -0.8903682039297,
                -0.26994815318748,
                 0.602248517290195,
                 0.980011992233124,
                 0.436594293214176
                };
            string _ss = @"50000000000 00000000000 20000000000 10000000000 A0000000000
30000000000 20000000000 00000000000 A0000000000 30000000000
00000000000 20000000000 A0000000000 60000000000 00000000000
00000000000 00000000000 00000000000 00000000000 00000000000
00000000m_3 00000000000 00000000000 00000000m_3 00000000000
00000000000 00000000004 00000000000 00000000000 00000000004
00000000000 00000000000 00000000804 00000000000 00000000000
00000000804 00000000000 00000000000 00000000G04 00000000000
00000000000 00000000G04 00000000000 00000000000 00000000O04
00000000000 00000000000 00000000O04 00000000000 00000000000
00000000S04 00000000000 00000000000 00000000S04 00000000000
00000000000 00000000W04 00000000000 00000000000 00000000W04
00000000000 00000000000 00000000Y04 00000000000 00000000000
00000000Y04 00000000000 00000000000 00000000K04 00000000000
00000000000 00000000K04 00000000000 00000000000 A0000000000
00000000000 10000000000 20000000000 30000000000 40000000000
60000000000 70000000000 80000000000 90000000000 50000000000
30000000000 00000000000 00000000000 00000000000 30000000000
00000000Y04 00000000000 00000000000 u1000000000 00000000000
00000000000 00000000000 60000000000 80000000000 00000000000
50000000000 00000000000 50000000000 50000000000 00000000000
00000000000 00000000000 00000000000 00000000000 00000000000
00000000000 00000000000 00000000000 00000000000 00000000000
00000000000 00000000000 00000000000 00000000000 00000000000
00000000000 00000000000 00000000000 00000000000 00000000000
00000000000 00000000000 00000000000 00000000000 00000000000
00000000000 00000000000 00000000000 00000000000 00000000000
00000000000 00000000000 00000000000 00000000000 00000000000
00000000000 00000000000 00000000000 00000000000 00000000000
00000000000 00000000000 00000000000 00000000000 00000000000
00000000000 00000000000 00000000000 00000000000 00000000000
00000000000 00000000000 00000000000 00000000000 00000000000
00000000000 00000000000 00000000000 00000000000 00000000000
00000000000 00000000000 00000000000 00000000000 00000000000
00000000000 00000000000 00000000000 00000000000 00000000000
00000000000 00000000000 00000000000 00000000000 00000000000
00000000000 00000000000 00000000000 00000000000 00000000000
00000000000 00000000000 00000000000 00000000000 00000000000
00000000000 00000000000 00000000000 00000000000 00000000000
00000000000 00000000000 00000000000 00000000000 00000000000
00000000000 00000000000 00000000000 00000000000 00000000000
00000000000 00000000000 00000000000 00000000000 00000000000
00000000000 00000000000 00000000000 00000000000 K0000000000
00000000I04 00000000000 00000000000 00000000000 00000000000
00000000000 00000000000 00000000000 00000000000 00000000000
00000000000 00000000000 00000000000 00000000000 00000000000
00000000000 00000000000 00000000000 00000000000 00000000000
A0000000000 30000000000 00000000000 00000000000 00000000000
00000000m_3 00000000000 00000000000 00000000004 00000000000
00000000000 00000000804 00000000000 00000000000 00000000G04
00000000000 00000000000 00000000K04 00000000000 00000000000
00000000O04 00000000000 00000000000 00000000S04 00000000000
00000000000 00000000W04 00000000000 00000000000 00000000Y04
00000000000 00000000000 A0000000000 40000000000 00000000q04
-pAGQnQBI14 UqUWierJ91C esm8ag6G61C 00000000q04 4wcFMyCtu04
oPDvwHqst04 CExQXp8Ct04 00000000q04 litzPFhRb0C oKJvjcct314
5-fT-X8w614 00000000q04 3HSOsPVH11C vZWf4dgfv04 GbZg4MTJn04
00000000q04 iv7rMhuR71C hRtixp15r_3 EvCEDtLu-0C 00000000q04
41CXzA_q71C umRYLK2yp0C 1zzY3Zqd91C 00000000q04 JvxJzDeI21C
TVbyd7Ygz0C JLywRdR1n0C 00000000q04 KmFarhc4g0C 1ehrn2tUt0C
AECfwTIX814 00000000q04 Big__6hwt04 nSPzmAQrh_B 2H3o-KftH14
00000000q04 n1b9361vI14 mhJhviUE114 54a_qyBrH1C 00000000q04
10000000000 40000000000 StLCgor39-3 00000000000 00000000000
6qTG7Ae-1_3
";

            // test string unserialization without trailing dot symbol (end-of-stream marker); must work
            try
            {
                string s = _ss;
                alglib.rbfmodel model;
                alglib.rbfunserialize(s, out model);
                _TestResult = true;
                for(int i=0; i<ref_val.Length; i++)
                    _TestResult = _TestResult && (Math.Abs(alglib.rbfcalc2(model,i,0)-ref_val[i])<eps);
            }
            catch
            { _TestResult = false; }

            // test string unserialization with trailing dot symbol (end-of-stream marker); must work
            try
            {
                string s = _ss+".";
                alglib.rbfmodel model;
                alglib.rbfunserialize(s, out model);
                _TestResult = true;
                for(int i=0; i<ref_val.Length; i++)
                    _TestResult = _TestResult && (Math.Abs(alglib.rbfcalc2(model,i,0)-ref_val[i])<eps);
            }
            catch
            { _TestResult = false; }

            // test stream unserialization with trailing dot symbol (end-of-stream marker); must work
            try
            {
                System.IO.Stream s = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(_ss+"."));
                alglib.rbfmodel model;
                alglib.rbfunserialize(s, out model);
                _TestResult = true;
                for(int i=0; i<ref_val.Length; i++)
                    _TestResult = _TestResult && (Math.Abs(alglib.rbfcalc2(model,i,0)-ref_val[i])<eps);
            }
            catch
            { _TestResult = false; }

            // test stream unserialization with trailing dot symbol (end-of-stream marker); MUST FAIL
            try
            {
                System.IO.Stream s = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(_ss));
                alglib.rbfmodel model;
                alglib.rbfunserialize(s, out model);
                _TestResult = false; // must FAIL!
            }
            catch
            {  }
            
            //
            // Report
            //
            System.Console.WriteLine("* RBF bwd compatibility      "+(_TestResult ? " OK" : " FAILED"));
            _TotalResult = _TotalResult && _TestResult;
        }
        
        //
        // Performance tests
        //
        System.Console.WriteLine("Performance:");
        {
            {
                int[] _n = new int[]{ 16, 32, 64, 256, 1024, 0};
                int i, j, k, t, nidx;
                for(nidx=0; _n[nidx]!=0; nidx++)
                {
                    //
                    // Settings:
                    // * n - matrix size
                    // * nrepeat - number of repeated multiplications, always divisible by 4
                    //
                    int n = _n[nidx];
                    double desiredflops = n>64 ? 1.0E10 : 1.0E9;
                    int nrepeat = (int)(desiredflops/(2*System.Math.Pow(n,3.0)));
                    nrepeat = 4*(nrepeat/4+1);
                    
                    //
                    // Actual processing
                    //
                    double[,] a, b, c;
                    double perf0, perf1, perf2;
                    a = new double[n,n];
                    b = new double[n,n];
                    c = new double[n,n];
                    for(i=0; i<n; i++)
                        for(j=0; j<n; j++)
                        {
                            a[i,j] = alglib.math.randomreal()-0.5;
                            b[i,j] = alglib.math.randomreal()-0.5;
                            c[i,j] = 0.0;
                        }
                    
                    alglib.setnworkers(0);
                    t = System.Environment.TickCount;
                    for(k=0; k<nrepeat; k++)
                        alglib.rmatrixgemm(
                            n, n, n,
                            1.0,
                            a, 0, 0, k%2,
                            b, 0, 0, (k/2)%2,
                            0.0,
                            c, 0, 0);
                    t = System.Environment.TickCount-t;
                    perf0 = 1.0E-6*System.Math.Pow(n,3)*2.0*nrepeat/(0.001*t);
                    System.Console.WriteLine("* RGEMM-SEQ-{0,-4:D} (MFLOPS)  {1,5:F0}", n, perf0);
                    
                    alglib.setnworkers(0);
                    t = System.Environment.TickCount;
                    for(k=0; k<nrepeat; k++)
                        alglib.rmatrixgemm(
                            n, n, n,
                            1.0,
                            a, 0, 0, k%2,
                            b, 0, 0, (k/2)%2,
                            0.0,
                            c, 0, 0,
                            alglib.parallel);
                    t = System.Environment.TickCount-t;
                    perf2 = 1.0E-6*System.Math.Pow(n,3)*2.0*nrepeat/(0.001*t);
                    System.Console.WriteLine("* RGEMM-MTN-{0,-4:D}           {1,4:F1}x", n, perf2/perf0);
                }
            }
        }
        
        
        //
        // Test below creates instance of MemoryLeaksTest object.
        //
        // This object is descendant of CriticalFinalizerObject class,
        // which guarantees that it will be finalized AFTER all other
        // ALGLIB objects which hold pointers to unmanaged memory.
        //
        // Tests for memory leaks are done within object's destructor.
        //
        MemoryLeaksTest _test_object = new MemoryLeaksTest();
        if( !_TotalResult )
            System.Environment.ExitCode = 1;
    }
}
