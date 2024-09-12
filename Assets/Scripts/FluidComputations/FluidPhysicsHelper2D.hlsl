#define PI 3.14159265358979323846


// SPH kernels

float Poly6(float r, float h) {
    if (r >= h) { return 0.0; }
    
    float h2 = h * h;
    float h4 = h2 * h2;
    float r2 = r * r;
    float normFactor = 315.0 / (64.0 * PI * h4 * h4 * h);
    
    return normFactor * (h2 - r2) * (h2 - r2) * (h2 - r2);
}

float CubicSpline(float r, float h) {
    if (r >= h) { return 0.0; }

    float q = r / h;
    float q2 = q * q;
    float q3 = q2 * q;
    float normFactor = 8.0 / (PI * h * h * h);
    float rawKernelValue = 0.0;

    if (q <= 0.5) {
        rawKernelValue = 6.0 * (q3 - q2) + 1.0;
    }
    else {
        rawKernelValue = 2.0 * (1.0 - q) * (1.0 - q) * (1.0 - q);
    }

    return normFactor * rawKernelValue;
}

float Spiky(float r, float h) {
    if (r >= h) { return 0.0; }

    float h2 = h * h;
    float h6 = h2 * h2 * h2;
    float normFactor = 15.0 / (PI * h6);

    return normFactor * (h - r) * (h - r) * (h - r);
}

float WendlandQuinticC2(float r, float h) {
    if (r >= h) { return 0.0; }

    float q = r / h;
    float h2 = h * h;
    float normFactor = 21.0 / (2.0 * PI * h2 * h);

    return normFactor * (1.0 - q) * (1.0 - q) * (1.0 - q) * (1.0 - q) * (4.0 * q + 1.0);
}


// SPH Gradient kernels

float3 Poly6Gradient(float3 r, float h) {
    float h2 = h * h;
    float r2 = dot(r, r);

    if (r2 >= h2) { return 0.0; }

    float h4 = h2 * h2;
    float normFactor = - 945.0 / (32.0 * PI * h4 * h4 * h);

    return normFactor * (h2 - r2) * (h2 - r2) * r;
}

float3 CubicSplineGradient(float3 r, float h) {
    float h2 = h * h;
    float r2 = dot(r, r);

    if (r2 >= h2) { return 0.0; }

    float h3 = h2 * h;
    float rL = sqrt(r2);

    if (rL < 1e-10) { return float3(0.0, 0.0, 0.0); }

    float q = rL / h;
    float normFactor = 48.0 / (PI * h3);
    float rawKernelValue = 0.0;

    if (q <= 0.5) {
        rawKernelValue = q * (3.0 * q - 2.0);
    }
    else {
        rawKernelValue = - (1.0 - q) * (1.0 - q);
    }

    return normFactor * rawKernelValue * (r / (rL * h));
}

float3 SpikyGradient(float3 r, float h) {
    float r2 = dot(r, r);
    float h2 = h * h;
    
    if (r2 >= h2) { return 0.0; }

    float rL = sqrt(r2);

    if (rL < 1e-10) { return float3(0.0, 0.0, 0.0); }

    float h6 = h2 * h2 * h2;
    float normFactor = - 45.0 / (PI * h6);

    return normFactor * (h - rL) * (h - rL) * (r / rL);
}

float3 WendlandQuinticC2Gradient(float3 r, float h) {
    float r2 = dot(r, r);
    float h2 = h * h;

    if (r2 >= h2) { return 0.0; }

    float rL = sqrt(r2);

    if (rL < 1e-10) { return float3(0.0, 0.0, 0.0); }

    float h4 = h2 * h2;
    float q = rL / h;
    float normFactor = - 210.0 / (PI * h4);

    return normFactor * q * (1.0 - q) * (1.0 - q) * (1.0 - q) * (r / rL);
}


// SPK kernel gradient magnitudes

float Poly6GradientMagnitude(float r, float h) {
    if (r >= h) { return 0.0; }

    float r2 = r * r;
    float h2 = h * h;
    float h4 = h2 * h2;
    float normFactor = - 945.0 / (32.0 * PI * h4 * h4 * h);

    return normFactor * r * (h2 - r2) * (h2 - r2);
}

float CubicSplineGradientMagnitude(float r, float h) {
    if (r >= h) { return 0.0; }

    float h_1 = 1.0 / h;
    float h3_1 = h_1 * h_1 * h_1;
    float q = r * h_1;
    float normFactor = (48.0 * h3_1) / PI;
    float rawKernelValue = 0.0;

    if (q <= 0.5) {
        rawKernelValue = q * (3.0 * q - 2.0);
    }
    else {
        rawKernelValue = - (1.0 - q) * (1.0 - q);
    }

    return normFactor * rawKernelValue * h_1;
}

float SpikyGradientMagnitude(float r, float h) {
    if (r >= h) { return 0.0; }

    float h2 = h * h;
    float h6 = h2 * h2 * h2;
    float normFactor = - 45.0 / (PI * h6);

    return normFactor * (h - r) * (h - r);
}

float WendlandQuinticC2GradientMagnitude(float r, float h) {
    if (r >= h) { return 0.0; }

    float h_1 = 1.0 / h;
    float h2_1 = h_1 * h_1;
    float h4_1 = h2_1 * h2_1;
    float q = r * h_1;
    float normFactor = - (210.0 * h4_1) / PI;

    return normFactor * q * (1.0 - q) * (1.0 - q) * (1.0 - q);
}


// SPH kernel encapsulators

float DensityKernel(float r, float h) {
    return Poly6(r, h);
}

float3 DensityGradientKernel(float3 r, float h) {
    return Poly6Gradient(r, h);
}


// Pressure helpers

float ColeStateEquation(float density, float restDensity, float k, float gamma) {
    return k * (pow(density / restDensity, gamma) - 1);
}

float LinearStateEquation(float density, float restDensity, float k) {
    return k * (density - restDensity);
}