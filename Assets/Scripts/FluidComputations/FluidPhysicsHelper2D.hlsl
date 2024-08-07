#define PI 3.14159265358979323846


// SPH kernels

float CubicSpline(float r, float h) {
    float q = r / h;

    // Check values outside support range
    if (q >= 1.0) { return 0.0; }

    float q2 = q * q;
    float q3 = q2 * q;

    // Normalization factor for 2D
    float normFactor = 40.0 / (7.0 * PI * h * h);
    float baseValue = 0.0;

    if (q > 0.5) {
        baseValue = 2.0 * (1.0 - q) * (1.0 - q) * (1.0 - q);
    }
    else if (q > 0.0) {
        baseValue = 6.0 * (q3 - q2) + 1.0;
    }
    return normFactor * baseValue;
}

float QuinticSpline(float r, float h) {

}

float Gaussian(float r, float h) {

}

float WendlandQuinticC2(float r, float h) {

}



// SPH kernel encapsulators

float DensityKernel(float r, float h) {
    return CubicSpline(r, h);
}