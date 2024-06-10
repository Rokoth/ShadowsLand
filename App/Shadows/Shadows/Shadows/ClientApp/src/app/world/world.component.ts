import { Component, ElementRef, Inject, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormGroup, FormControl, ReactiveFormsModule } from '@angular/forms';


@Component({
  selector: 'world',
  templateUrl: './world.component.html'
})
export class WorldComponent {
  public data: World;
  private http: HttpClient;
  private baseUrl: string;
  gl: WebGLRenderingContext;

  @ViewChild('viewport', { static: true }) viewport: ElementRef;

  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this.http = http;
    this.baseUrl = baseUrl;   
  }

  ngOnInit() {
    console.log('ngOnInit');
   
    this.GetWorld();
    this.DrawWorld();

  }

  private GetWorld() {
    this.http.get<World>(this.baseUrl + 'world/getitem').subscribe(result => {
      this.data = result;
    }, error => console.error(error));
  }

  private DrawWorld() {
    const glOptions = {
      antialias: true,
      transparent: false
    };

    const canvas = this.viewport.nativeElement;
    this.gl = canvas.getContext('webgl', glOptions) ||
      canvas.getContext('experimental-webgl', glOptions);

    if (!this.gl) {
      console.log('No WebGL support found.');
      return;
    }

    this.initViewport();
    this.clearViewport();
    this.testShader();

  }

  initViewport(): void {
    this.gl.viewport(0, 0,
      this.viewport.nativeElement.width,
      this.viewport.nativeElement.height);
  }

  clearViewport(): void {
    this.gl.clearColor(0, 0, 0, 1);
    this.gl.clear(this.gl.COLOR_BUFFER_BIT);
  }

  testShader(): void {
    const vert = "\
      attribute vec4 aPosition; \
      void main () {            \
        gl_Position = aPosition;\
        gl_PointSize = 10.0;    \
      }";
    const frag = "\
      precision mediump float;    \
      uniform vec4 uFragColor;    \
      void main () {              \
        gl_FragColor = uFragColor;\
      }";

    const vsh = this.compileShader(this.gl.VERTEX_SHADER, vert);
    const fsh = this.compileShader(this.gl.FRAGMENT_SHADER, frag);
    const prog = this.linkShaders(vsh, fsh);
    this.gl.useProgram(prog);
    const aPosition = this.gl.getAttribLocation(prog, 'aPosition');
    const uFragColor = this.gl.getUniformLocation(prog, 'uFragColor');
    this.gl.vertexAttrib2f(aPosition, 0.0, 0.0);
    this.gl.uniform4f(uFragColor, 1.0, 0.0, 0.0, 1.0);
    this.gl.drawArrays(this.gl.POINTS, 0, 1);
  }

  compileShader(type, shaderSrc): any {
    const shader = this.gl.createShader(type);
    this.gl.shaderSource(shader, shaderSrc);
    this.gl.compileShader(shader);

    if (!this.gl.getShaderParameter(shader, this.gl.COMPILE_STATUS)) {
      throw new Error(this.gl.getShaderInfoLog(shader));
    }

    return shader;
  }

  linkShaders(vertexShader, fragmentShader): any {
    const program = this.gl.createProgram();
    this.gl.attachShader(program, vertexShader);
    this.gl.attachShader(program, fragmentShader);
    this.gl.linkProgram(program);

    if (!this.gl.getProgramParameter(program, this.gl.LINK_STATUS)) {
      throw new Error(this.gl.getProgramInfoLog(program));
    }

    return program;
  }
  
}

interface World {
  name: string;
  locations: Location[];
}

interface Location {
  name: string;
}
