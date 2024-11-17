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

struct AABB {
    float2 min;
    float2 max;
};

struct SimulationCollider {
    int startIdx;
    int numSides;
    int isBounds;
    AABB aabb;
};

struct CollisionDisplacement {
    float magnitude;
    float2 direction;
};

struct HollowCollisionDisplacement {
    float2 displacement;
    float2 normal;
    float2 quadrant;
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


// Collision responses

float GetCollisionResponseOverLine(ProjectionLine particleProjection, ProjectionLine colliderProjection) {
    if (particleProjection.start >= colliderProjection.end || particleProjection.end <= colliderProjection.start) {
        return 0.0;
    }

    return colliderProjection.end - particleProjection.start;
}

CollisionDisplacement GetSolidCollisionResponse(ParticleCollider particle, int colliderIdx) {
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

float2 GetVelocityAdjustment(float2 velocity, float2 quadrant, float2 gravity) {
    float2 isGravityOnAxis = step(abs(gravity), float2(0.0, 0.0));
    float2 isVelocitySmall = abs(velocity) < 0.001;
    return - isGravityOnAxis * isVelocitySmall * quadrant * 0.1;
}

HollowCollisionDisplacement GetHollowCollisionResponse(ParticleCollider particle, int colliderIdx) {
    HollowCollisionDisplacement collisionResponse = { float2(0.0, 0.0), float2(0.0, 0.0), float2(0.0, 0.0) };
    SimulationCollider collider = _CollidersLookup[colliderIdx];
    float2 vertexMaxMax = _CollidersVertices[collider.startIdx];
    float2 vertexMinMin = _CollidersVertices[collider.startIdx + 2];
    float2 boundsHalfSize = (vertexMaxMax - vertexMinMin) * 0.5 - particle.radius;
    float2 colliderCentre = vertexMinMin + boundsHalfSize + particle.radius;
    
    float2 distCentreToParticle = particle.position - colliderCentre;
    float2 quadrant = sign(distCentreToParticle);
    float2 dst = boundsHalfSize - quadrant * distCentreToParticle;

    float2 needsToBounce = step(float2(0.0, 0.0), - dst);
    collisionResponse.displacement = (quadrant * boundsHalfSize + colliderCentre - particle.position) * needsToBounce; 
    collisionResponse.normal = - quadrant * needsToBounce;
    collisionResponse.quadrant = quadrant;

    return collisionResponse;
}