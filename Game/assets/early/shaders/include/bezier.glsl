
struct bezier_t
{
	vec3 p0;
	vec3 p1;
	vec3 p2;
	vec3 p3;
};

vec3 sample_bezier(bezier_t bezier, float t)
{
	float oneMinusT = 1.0 - t;
	return
		oneMinusT * oneMinusT * oneMinusT * bezier.p0 +
		3.0 * oneMinusT * oneMinusT * t * bezier.p1 +
		3.0 * oneMinusT * t * t * bezier.p2 +
		t * t * t * bezier.p3;
}

vec3 sample_bezier_derivative(bezier_t bezier, float t)
{
	float oneMinusT = 1.0 - t;
	return
		3.0 * oneMinusT * oneMinusT * (bezier.p1 - bezier.p0) +
		6.0 * oneMinusT * t * (bezier.p2 - bezier.p1) +
		3.0 * t * t * (bezier.p3 - bezier.p2);
}
