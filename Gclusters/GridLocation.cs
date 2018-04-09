using static System.Math;
using System;

namespace Gclusters
{
    internal class GridLocation
    {
        class EEllipse
        {
            public double A { get; set; }
            public double B { get; set; }
            public double F { get; set; }
        };

        // Helmert Transformation - see http://en.wikipedia.org/wiki/Helmert_transformation
        class Helmert
        {
            public double Tx { get; set; }
            public double Ty { get; set; }
            public double Tz { get; set; }
            public double Rx { get; set; }
            public double Ry { get; set; }
            public double Rz { get; set; }
            public double S { get; set; }
        };

        private double _longitude;      // longitude in WGS84
        private double _latitude;       // latitude in WGS84
        private bool _cacheValid;       // are calculated eastings and northings still valid?
        private int _cachedEastings;    // cached eastings - we don't want to recalculate unnecesarily as calculation is LONG!
        private int _cachedNorthings;   // cached northings - we don't want to recalculate unnecesarily as calculation is LONG!

        private const string squares = "SV.SW.SX.SY.SZ.TV.XX.SQ.SR.SS.ST.SU.TQ.TR.XX.SM.SN.SO.SP.TL.TM.XX.XX.SH.SJ.SK.TF.TG.XX.XX.SC.SD.SE.TA.XX.XX.NW.NX.NY.NZ.XX.XX.NQ.NR.NS.NT.NU.XX.XX.NL.NM.NN.NO.NP.XX.XX.NF.NG.NH.NJ.NK.XX.XX.NA.NB.NC.ND.NE.XX.XX.HV.HW.HX.HY.HZ.XX.XX.HQ.HR.HS.HT.HU.XX.XX.HL.HM.HN.HO.HP.XX.XX.";

        public (int eastings, int northings) OsGrid
        {
            get
            {
                if (!_cacheValid)
                {
                    var (newLat, newLong) = ConvertWGS84ToOSGB36(_latitude, _longitude);
                    LatLongToOsGrid(newLat, newLong);
                }
                return (_cachedEastings, _cachedNorthings);
            }
        }

        public int Eastings
        {
            get
            {
                if (!_cacheValid)
                {
                    var (newLat, newLong) = ConvertWGS84ToOSGB36(_latitude, _longitude);
                    LatLongToOsGrid(newLat, newLong);
                }
                return _cachedEastings;
            }
        }

        public int Northings
        {
            get
            {
                if (!_cacheValid)
                {
                    var (newLat, newLong) = ConvertWGS84ToOSGB36(_latitude, _longitude);
                    LatLongToOsGrid(newLat, newLong);
                }
                return _cachedNorthings;
            }
        }

        string GetSquare()
        {
            if (!_cacheValid)
            {
                var (newLat, newLong) = ConvertWGS84ToOSGB36(_latitude, _longitude);
                LatLongToOsGrid(newLat, newLong);
            }
            int hsquare = _cachedEastings / 100000;
            int vsquare = _cachedNorthings / 100000 - 1;

            int index = 3 * (vsquare * 7 + hsquare);
            return $"{squares[index]}{squares[index + 1]}";
        }

        int GetEastingsInSquare()
        {
            return Eastings % 100000;
        }

        int GetNorthingsInSquare()
        {
            return Northings % 100000;
        }

        private (double latitude, double longitude) ConvertSystem(EEllipse eTo, EEllipse eFrom, Helmert h, double latitude, double longitude)
        {
            double a = eFrom.A, b = eFrom.B;
            double f = eFrom.F;
            double H = 0; // Height

            double eSq = 2 * f - f * f;
            double eSq1 = (a * a - b * b) / (a * a);
            double nu = a / Sqrt(1 - eSq * Sin(latitude) * Sin(latitude));
            double x1 = (nu + H) * Cos(latitude) * Cos(longitude);
            double y1 = (nu + H) * Cos(latitude) * Sin(longitude);
            double z1 = ((1 - eSq) * nu + H) * Sin(latitude);

            // Console.WriteLine($"old cartesian {x1:N11}, {y1:N11}, {z1:N11}");

            // Apply helmert transformation:
            double tx = h.Tx, ty = h.Ty, tz = h.Tz;
            double rx = h.Rx / 3600 * PI / 180;  // convert seconds to radians
            double ry = h.Ry / 3600 * PI / 180;
            double rz = h.Rz / 3600 * PI / 180;
            double s1 = h.S / 1e6 + 1;         // convert ppm to (s+1)

            // apply transform
            double x2 = tx + x1 * s1 - y1 * rz + z1 * ry;
            double y2 = ty + x1 * rz + y1 * s1 - z1 * rx;
            double z2 = tz - x1 * ry + y1 * rx + z1 * s1;

            // Console.WriteLine($"new cartesian {x2:N11}, {y2:N11}, {z2:N11}");
            a = eTo.A;
            b = eTo.B;
            f = eTo.F;

            eSq = 2 * f - f * f;
            nu = a / Sqrt(1 - eSq * Sin(latitude) * Sin(latitude));
            double p = Sqrt(x2 * x2 + y2 * y2);
            double phi = Atan2(z2, p * (1 - eSq));
            double phiP = 2 * PI;

            double precision = 0.001 / a;  // results accurate to around 4 metres
            var iterationCount = 0;

            while (Abs(phi - phiP) > precision)
            {
                nu = a / Sqrt(1 - eSq * Sin(phi) * Sin(phi));
                phiP = phi;
                phi = Atan2(z2 + eSq * nu * Sin(phi), p);
                iterationCount++;
            }
            // Console.WriteLine($"Iterations: {iterationCount}");
            double lambda = Atan2(y2, x2);
            H = p / Cos(phi) - nu;

            return (phi, lambda);
        }

        private (double latitude, double longitude) ConvertWGS84ToOSGB36(double latitude, double longitude)
        {
            var WGS84Ellipse = new EEllipse { A = 6378137, B = 6356752.314245, F = 1 / 298.257223563 };
            var OSGB36Ellipse = new EEllipse { A = 6377563.396, B = 6356256.909, F = 1 / 299.3249646 };
            Helmert h = new Helmert
            {
                Tx = -446.448,
                Ty = 125.157,
                Tz = -542.060,   // tx, ty, tz in metres (translation parameters)
                Rx = -0.1502,
                Ry = -0.2470,
                Rz = -0.8421,    // rx, ry, rz in seconds of a degree (rotational parameters)
                S = 20.4894      // scale;
            };
            return ConvertSystem(OSGB36Ellipse, WGS84Ellipse, h, latitude, longitude);
        }

        /// <summary>
        /// Adjust the latitude and longitude of this instance from OSGB36 (the coordinates used for grid referenced) to
        /// the coordinates used by GBP (WGS84)
        /// </summary>
        private (double latitude, double longitude) ConvertOSGB36ToWgs84(double latitude, double longitude)
        {
            var WGS84Ellipse = new EEllipse { A = 6378137, B = 6356752.314245, F = 1 / 298.257223563 };
            var OSGB36Ellipse = new EEllipse { A = 6377563.396, B = 6356256.909, F = 1 / 299.3249646 };
            Helmert helmert = new Helmert
            {
                Tx = 446.448,
                Ty = -125.157,
                Tz = 542.060,   // tx, ty, tz in metres (translation parameters)
                Rx = 0.1502,
                Ry = 0.2470,
                Rz = 0.8421,    // rx, ry, rz in seconds of a degree (rotational parameters)
                S = -20.4894      // scale;
            };
            return ConvertSystem(WGS84Ellipse, OSGB36Ellipse, helmert, latitude, longitude);
        }


        public static GridLocation CreateFromDegrees(double latitude, double longitude)
        {
            var result = new GridLocation();
            result.SetLatLongDegrees(latitude, longitude);
            return result;
        }
        public static GridLocation CreateFromOsGrid(int eastings, int northings)
        {
            var result = new GridLocation();
            result.SetFromOsGrid(eastings, northings);
            return result;
        }

        /// <summary>
        /// Set latitude and longitude from degrees. The values specified should be in WGS84 coordinates
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns></returns>
        public bool SetLatLongDegrees(double latitude, double longitude)
        {
            bool result;
            if (latitude < -90.0000000001 || latitude > 90.0000000001 ||
                longitude < -360.0000000001 || longitude > 360.0000000001)
            {
                result = false;
            }
            else
            {
                _cacheValid = false;   // we have changed so E/N cache is rubbish
                _latitude = latitude * Math.PI / 180.0;
                _longitude = longitude * Math.PI / 180.0;
                result = true;
            }

            return result;
        }

        public void SetFromOsGrid(double E, double N)
        {
            // const double n = 0.001673220250;     // ellipsoidal constant - see textbooks
            const double a = 6375020.481;           // ellipsoidal constant - see textbooks (scaled by f0)
            const double b = 6353722.490;           // ellipsoidal constant - see textbooks (scaled by f0)

            const double phi0 = 49 * PI / 180;      // True origin of latitude - 49 deg. north
            const double lambda0 = -2 * PI / 180;   // True origin of longitude - 2 deg. west
            const double N0 = -100000;              // Grid northings of true origin
            const double E0 = 400000;               // Grid eastings of true origin

            double phidash, m, error, VII, VIII, IX, X, XI, XII, XIIA;

            double Et, t, t2, t4, t6;
            double ro, nu, phi;

            Et = E - E0;

            phidash = (N - N0) / a + phi0;

            int count = 0;
            do
            {
                m = fM(phi0, phidash);
                phidash = phidash + (N - N0 - m) / a;
                error = N - N0 - m;
                count++;
            }
            while (error > 0.000000001 && count < 40);

            //Console.WriteLine($"Count is {count} phidash = {phidash}");

            // eccentricity squared:
            var e2 = (a * a - b * b) / (a * a);

            nu = a / Sqrt(1.0 - e2 * Sin(phidash) * Sin(phidash));
            ro = nu * (1.0 - e2) / (1.0 - e2 * Sin(phidash) * Sin(phidash));
            var eta2 = nu / ro - 1;

            t = Tan(phidash);
            t2 = t * t;
            t4 = t * t * t * t;
            t6 = t * t * t * t * t * t;

            VII = t / (2 * ro * nu);
            VIII = t * (5 + 3 * t2 + eta2 - 9 * t2 * eta2) / (24 * ro * nu * nu * nu);
            IX = t * (61 + 90 * t2 + 45 * t4) / (720 * ro * nu * nu * nu * nu * nu);

            phi = phidash - Et * Et * VII +
                            Et * Et * Et * Et * VIII -
                            Et * Et * Et * Et * Et * Et * IX;

            X = 1 / (Cos(phidash) * nu);
            XI = (nu / ro + 2 * t2) / (6 * nu * nu * nu * Cos(phidash));
            XII = (5 + 28 * t2 + 24 * t4) / (120 * nu * nu * nu * nu * nu * Cos(phidash));
            XIIA = (61 + 662 * t2 + 1320 * t4 + 720 * t6) / (5040 * nu * nu * nu * nu * nu * nu * nu * Cos(phidash));

            //Console.WriteLine($"VII is {VII}");
            //Console.WriteLine($"VIII is {VIII}");
            //Console.WriteLine($"IX is {IX}");
            //Console.WriteLine($"XII is {XII}");
            //Console.WriteLine($"XIIA is {XIIA}");

            double lambda = lambda0 +
                            Et * X -
                            Et * Et * Et * XI +
                            Et * Et * Et * Et * Et * XII -
                            Et * Et * Et * Et * Et * Et * Et * XIIA;

            //Console.WriteLine($"lamba is {lambda} phi is {phi}");
            //Console.WriteLine($"pretransform: _longitude is {_longitude} latitude is {_latitude}");
            //Console.WriteLine($"pretransform-degrees: _longitude is {_longitude * 180 / PI} latitude is {_latitude * 180 / PI}");

            (_latitude, _longitude) = ConvertOSGB36ToWgs84(phi, lambda);

            //Console.WriteLine($"posttransform: _longitude is {_longitude} latitude is {_latitude}");
            //Console.WriteLine($"posttransform-degrees: _longitude is {_longitude * 180 / PI} latitude is {_latitude * 180 / PI}");
        }

        /// <summary>
        /// Convert latitude and longitude to OS Grid
        /// </summary>
        /// <param name="phi">Latitude in radians</param>
        /// <param name="lambda">Longitude in radians</param>
        private void LatLongToOsGrid(double phi, double lambda)
        {
            const double PI = 3.14159265358979310;
            //            const double n = 0.001673220250; // ellipsoidal constant - see textbooks
            const double a = 6375020.481;    // ellipsoidal constant - see textbooks (scaled by f0)
            const double b = 6353722.490;    // ellipsoidal constant - see textbooks (scaled by f0)

            const double phi0 = 49 * PI / 180;      // True origin of latitude - 49 deg. north
            const double lambda0 = -2 * PI / 180;   // True origin of longitude - 2 deg. west
            const double N0 = -100000;              // Grid northings of true origin
                                                    //            const double E0 = 400000;             // Grid eastings of true origin

            // trigs and powers:
            var s = Sin(phi);
            var s2 = s * s;
            var c = Cos(phi);
            var c3 = c * c * c;
            var c5 = c * c * c * c * c;
            var t = Tan(phi);
            var t2 = t * t;
            var t4 = t2 * t2;

            var m = fM(phi0, phi);

            var e = Sqrt((a * a - b * b) / (a * a));
            var nu = a / Sqrt(1.0 - e * e * s2);
            var ro = nu * (1.0 - e * e) / (1.0 - e * e * s2);
            var eta = Sqrt(nu / ro - 1);
            var P = lambda - lambda0;

            var term1 = 5.0 - t2 + 9.0 * eta * eta;
            var term2 = 61.0 - 58.0 * t2 + t4;

            var I = m + N0;
            var II = nu * s * c / 2.0; //OK
            var III = nu * s * c3 * term1 / 24.0; //OK
            var IIIA = nu * s * c5 * term2 / 720;

            var N = I +
                    P * P * II +
                    P * P * P * P * III +
                    P * P * P * P * P * P * IIIA;

            var IV = nu * c;
            var V = nu * c3 * (nu / ro - t2) / 6;
            var VI = nu * c5 * (5 - 18 * t2 + t4 + 14 * eta * eta - 58 * t2 * eta * eta) / 120;

            var E = 400000 + P * IV + P * P * P * V + P * P * P * P * P * VI;

            // convert eastings an northings to integers in metres but remember to round up or down!:
            _cachedEastings = (int)(E + 0.5);
            _cachedNorthings = (int)(N + 0.5);

            // these values should be cached in case asked for again - setting a new value for latitude or longitude
            // will invalidate the cache:
            _cacheValid = true;
        }

        double fM(double phi1, double phi2)
        {
            const double b = 6353722.490;    // ellipsoidal constant - see textbooks (scaled by f0)
            const double n = 0.001673220250; // ellipsoidal constant - see textbooks [n is actually (a-b)/(a+b)]

            double f1, f2, f3, f4;
            var n2 = n * n;
            var n3 = n * n * n;
            var delta = phi2 - phi1;
            var sum = phi2 + phi1;

            f1 = (1.0 + n + (5.0 * (n2 + n3) / 4.0)) * delta;
            f2 = (3.0 * (n + n2) + 21.0 * n3 / 8) * Sin(delta) * Cos(sum);
            f3 = (15.0 * (n2 + n3) / 8.0) * Sin(2 * delta) * Cos(2 * sum);
            f4 = (35.0 * n * n * n / 24.0) * Sin(3 * delta) * Cos(3 * sum);

            return b * (f1 - f2 + f3 - f4);
        }

        public double Latitude => _latitude;
        public double Longitude => _longitude;
        public (double latitude, double longitude) LatLong => (_latitude * 180.0 / PI, _longitude * 180.0 / PI);

        public static double operator -(GridLocation loc1, GridLocation loc2)
        {
            var R = 6371e3; // metres
            var φ1 = loc1._latitude;
            var φ2 = loc2._latitude;
            var Δφ = (φ2 - φ1);
            var Δλ = loc1._longitude - loc2._longitude;

            var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                    Math.Cos(φ1) * Math.Cos(φ2) *
                    Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var d = R * c;
            return d;
        }

        private GridLocation()
        {
        }
    }
}