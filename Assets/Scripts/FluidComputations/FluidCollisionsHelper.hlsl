#define FLOAT_MAX 3.402823e+38
#define FLOAT_MIN (- FLOAT_MAX)


// Structs

struct ProjectionLine {
    float start;
    float end;
};

struct ParticleCollider {
    float2 position;
    float radius;
};

struct SimulationCollider {
    int startIdx;
    int numSides;
    int isBounds;
};

struct CollisionDisplacement {
    float magnitude;
    float2 direction;
};


// Buffers
RWStructuredBuffer<SimulationCollider> _CollidersLookup;
RWStructuredBuffer<float2> _CollidersVertices;
RWStructuredBuffer<float2> _CollidersNormals;


// Vector Math Helpers

float2 ReflectAndDampVelocity(float2 velocity, float2 normal, float collisionDamping) {
    return velocity - (collisionDamping + 1) * dot(velocity, normal) * normal;
}


// Projection Helpers

ProjectionLine ProjectCollider(SimulationCollider collider, float2 normal) {
    ProjectionLine colliderProjection = { FLOAT_MAX, FLOAT_MIN };
    
    for (int i = 0; i < collider.numSides; i++) {
        float vertexProjection = dot(_CollidersVertices[collider.startIdx + i], normal);
        colliderProjection.start = min(vertexProjection, colliderProjection.start);
        colliderProjection.end = max(vertexProjection, colliderProjection.end);
    }

    return colliderProjection;
}

ProjectionLine ProjectParticle(ParticleCollider particle, float2 normal) {
    float particleCentreProjection = dot(particle.position, normal);
    ProjectionLine particleProjection =  { particleCentreProjection - particle.radius, particleCentreProjection + particle.radius};
    return particleProjection;
}


// SAT Implementation (for filled colliders)

float GetCollisionResponseOverLine(ProjectionLine particleProjection, ProjectionLine colliderProjection) {
    if (particleProjection.start >= colliderProjection.end || particleProjection.end <= colliderProjection.start) {
        return 0.0;
    }

    return colliderProjection.end - particleProjection.start;
}

CollisionDisplacement GetCollisionResponse(ParticleCollider particle, int colliderIdx) {
    SimulationCollider collider = _CollidersLookup[colliderIdx];
    CollisionDisplacement collisionResponse = { FLOAT_MAX, float2(0.0, 0.0) };

    for (int i = 0; i < collider.numSides; i++) {
        float2 edgeNormal = _CollidersNormals[collider.startIdx + i];
        ProjectionLine particleProjection = ProjectParticle(particle, edgeNormal);
        ProjectionLine colliderProjection = ProjectCollider(collider, edgeNormal);
        float responseMagnitude = GetCollisionResponseOverLine(particleProjection, colliderProjection);

        // If there is no overlap
        if (responseMagnitude == 0.0) {
            collisionResponse.magnitude = 0.0;
            return collisionResponse;
        }
        
        // If there is a potential overlap
        if (abs(responseMagnitude) < abs(collisionResponse.magnitude)) {
            collisionResponse.magnitude = responseMagnitude;
            collisionResponse.direction = edgeNormal;
        }
    }

    return collisionResponse;
}